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

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenService jwtTokenService,
        DataContext context,
        IEmailQueueService emailQueue)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _emailQueue = emailQueue;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null) return Conflict("Email already registered.");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);


        await _userManager.AddToRoleAsync(user, AppRoles.Guest);

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            AvatarUrl = null,
            PreferredLanguage = "en",
            LoyaltyBalance = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        await SendVerificationEmailAsync(user, request.Email, CancellationToken.None);

        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized("Invalid email or password.");

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
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return NoContent();

        if (user.EmailConfirmed) return NoContent();

        var sent = await SendVerificationEmailAsync(user, request.Email, ct);
        if (!sent) return StatusCode(StatusCodes.Status429TooManyRequests, "Please wait before requesting a new code.");

        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Email and code are required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return BadRequest("Invalid or expired code.");

        var now = DateTime.UtcNow;
        var record = await _context.Set<EmailVerification>()
            .Where(ev => ev.UserId == user.Id && ev.Email == request.Email && ev.VerifiedAt == null)
            .OrderByDescending(ev => ev.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (record is null || record.ExpiresAt <= now)
            return BadRequest("Invalid or expired code.");

        var hash = HashCode(request.Code);
        if (!string.Equals(record.CodeHash, hash, StringComparison.Ordinal))
        {
            record.AttemptCount += 1;
            await _context.SaveChangesAsync(ct);
            return BadRequest("Invalid or expired code.");
        }

        record.VerifiedAt = now;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
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

    private async Task<bool> SendVerificationEmailAsync(AppUser user, string email, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var existing = await _context.Set<EmailVerification>()
            .FirstOrDefaultAsync(ev => ev.UserId == user.Id && ev.Email == email && ev.VerifiedAt == null, ct);

        if (existing?.LastSentAt is not null && existing.LastSentAt.Value.AddSeconds(60) > now)
            return false;

        var code = GenerateCode();
        var codeHash = HashCode(code);

        if (existing is null)
        {
            existing = new EmailVerification
            {
                UserId = user.Id,
                Email = email
            };
            _context.Set<EmailVerification>().Add(existing);
        }

        existing.CodeHash = codeHash;
        existing.ExpiresAt = now.AddMinutes(15);
        existing.CreatedAt = now;
        existing.AttemptCount = 0;
        existing.LastSentAt = now;

        await _context.SaveChangesAsync(ct);

        await _emailQueue.PublishAsync(new EmailMessage
        {
            To = email,
            Subject = "RoomWise email verification",
            Body = $"Your verification code is {code}. It expires in 15 minutes.",
            UserId = user.Id
        }, ct);

        return true;
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
}
