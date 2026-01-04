using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using RoomWise.Api.Auth;
using RoomWise.Api.Data;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Messaging;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DataContext _context;
    private readonly IEmailQueueService _emailQueue;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenService jwtTokenService,
        DataContext context,
        IEmailQueueService emailQueue,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _emailQueue = emailQueue;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email is required.");

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null) return Conflict("Email already registered.");

        var tempUser = new AppUser
        {
            UserName = email,
            Email = email
        };

        var passwordErrors = await ValidatePasswordAsync(tempUser, request.Password);
        if (passwordErrors.Count > 0) return BadRequest(passwordErrors);

        var pending = await _context.Set<PendingRegistration>()
            .FirstOrDefaultAsync(p => p.Email == email);

        if (pending is null)
        {
            pending = new PendingRegistration
            {
                Email = email
            };
            _context.Set<PendingRegistration>().Add(pending);
        }

        pending.PasswordHash = _passwordHasher.HashPassword(tempUser, request.Password);
        pending.FirstName = request.FirstName;
        pending.LastName = request.LastName;
        pending.CreatedAt = DateTime.UtcNow;

        await IssuePendingCodeAsync(pending, CancellationToken.None);

        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized("Invalid email or password.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var pending = await _context.Set<PendingRegistration>()
                .AnyAsync(p => p.Email == email);
            if (pending)
                return StatusCode(StatusCodes.Status403Forbidden, "Email not verified.");
            return Unauthorized("Invalid email or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized("Invalid email or password.");

        if (!user.EmailConfirmed)
            return StatusCode(StatusCodes.Status403Forbidden, "Email not verified.");

        var token = await _jwtTokenService.CreateTokenAsync(user);
        var (refreshToken, refreshExpiresUtc) = await IssueRefreshTokenAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            token,
            refreshToken,
            refreshExpiresUtc,
            roles
        });
    }

    [HttpPost("request-email-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestEmailVerification(
        [FromBody] RequestEmailVerificationRequest request,
        CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && user.EmailConfirmed) return NoContent();

        var pending = await _context.Set<PendingRegistration>()
            .FirstOrDefaultAsync(p => p.Email == email, ct);

        if (pending is null) return NoContent();

        await IssuePendingCodeAsync(pending, ct);

        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Email and code are required.");

        var pending = await _context.Set<PendingRegistration>()
            .FirstOrDefaultAsync(p => p.Email == email, ct);

        if (pending is null)
            return BadRequest("Invalid or expired code.");

        var now = DateTime.UtcNow;
        if (pending.CodeExpiresAt <= now)
            return BadRequest("Invalid or expired code.");

        var hash = HashCode(request.Code);
        if (!string.Equals(pending.CodeHash, hash, StringComparison.Ordinal))
        {
            pending.AttemptCount += 1;
            await _context.SaveChangesAsync(ct);
            return BadRequest("Invalid or expired code.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PasswordHash = pending.PasswordHash
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        await _userManager.AddToRoleAsync(user, AppRoles.Guest);

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = pending.FirstName,
            LastName = pending.LastName,
            AvatarUrl = null,
            PreferredLanguage = "en",
            LoyaltyBalance = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.UserProfiles.Add(profile);
        _context.Set<PendingRegistration>().Remove(pending);
        await _context.SaveChangesAsync(ct);

        return Ok(new { message = "Email verified." });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var tokenRow = await _context.Set<IdentityUserToken<string>>()
            .FirstOrDefaultAsync(t =>
                t.LoginProvider == "RoomWise" &&
                t.Name == "RefreshToken" &&
                t.Value != null &&
                t.Value.StartsWith(request.RefreshToken + "|"));

        if (tokenRow is null) return Unauthorized("Invalid refresh token.");

        var parts = tokenRow.Value?.Split('|', 2);
        if (parts is null || parts.Length != 2 || !DateTime.TryParse(parts[1], out var expiresUtc))
            return Unauthorized("Invalid refresh token.");

        if (DateTime.UtcNow >= expiresUtc)
            return Unauthorized("Refresh token expired.");

        var user = await _userManager.FindByIdAsync(tokenRow.UserId);
        if (user is null) return Unauthorized("Invalid refresh token.");

        var accessToken = await _jwtTokenService.CreateTokenAsync(user);
        var (newRefresh, newRefreshExpiry) = await IssueRefreshTokenAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            token = accessToken,
            refreshToken = newRefresh,
            refreshExpiresUtc = newRefreshExpiry,
            roles
        });
    }

    private async Task<(string token, DateTime expiresUtc)> IssueRefreshTokenAsync(AppUser user)
    {
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expires = DateTime.UtcNow.AddDays(30);
        var value = $"{refreshToken}|{expires:o}";


        await _userManager.SetAuthenticationTokenAsync(user, "RoomWise", "RefreshToken", value);
        return (refreshToken, expires);
    }

    private async Task IssuePendingCodeAsync(PendingRegistration pending, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var code = GenerateCode();
        pending.CodeHash = HashCode(code);
        pending.CodeExpiresAt = now.AddMinutes(15);
        pending.AttemptCount = 0;
        pending.LastSentAt = now;

        await _context.SaveChangesAsync(ct);

        var body = new StringBuilder()
            .AppendLine("Welcome to RoomWise!")
            .AppendLine()
            .AppendLine("Please verify your email address using the code below:")
            .AppendLine()
            .AppendLine($"  {code}")
            .AppendLine()
            .AppendLine("This code expires in 15 minutes.")
            .AppendLine("If you didn't request this, you can safely ignore this email.")
            .AppendLine()
            .AppendLine("Thanks,")
            .AppendLine("RoomWise Team")
            .ToString();

        await _emailQueue.PublishAsync(new EmailMessage
        {
            To = pending.Email,
            Subject = "RoomWise email verification",
            Body = body,
            UserId = null
        }, ct);
    }

    private static string GenerateCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return value.ToString("D6");
    }

    private static string HashCode(string code)
    {
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private async Task<List<IdentityError>> ValidatePasswordAsync(AppUser user, string password)
    {
        var errors = new List<IdentityError>();
        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
                errors.AddRange(result.Errors);
        }
        return errors;
    }
}
