// Api/Controller/MeProfileController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;
using RoomWise.Model;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/me/profile")]
[Authorize]
public sealed class MeProfileController : ControllerBase
{
    private readonly IUserProfileService _profiles;
    private readonly UserManager<AppUser> _users;

    public MeProfileController(IUserProfileService profiles, UserManager<AppUser> users)
    {
        _profiles = profiles;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetMine(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var res = await _profiles.GetMineAsync(userId, ct);
        if (res is not null) return Ok(res);


        var created = await _profiles.UpsertMineAsync(userId, new UserProfileUpsertRequest
        {
            FirstName = User.Identity?.Name ?? string.Empty,
            LastName = string.Empty,
            AvatarUrl = null,
            PreferredLanguage = "en",
            Phone = null
        }, ct);

        return Ok(created);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> UpsertMine([FromBody] UserProfileUpsertRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var res = await _profiles.UpsertMineAsync(userId, req, ct);
        return Ok(res);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Forbid();

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { errors });
        }

        return NoContent();
    }

    [HttpGet("debug-auth")]
    [Authorize]
    public IActionResult DebugAuth()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            Name = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }


}
