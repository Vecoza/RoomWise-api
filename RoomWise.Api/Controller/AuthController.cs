using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomWise.Api.Auth;
using RoomWise.Api.Data;
using RoomWise.Model;
using RoomWise.Model.Requests;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DataContext _context;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenService jwtTokenService,
        DataContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
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

        // roles already exist from seeding
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

        // store in AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(user, "RoomWise", "RefreshToken", value);
        return (refreshToken, expires);
    }
}
