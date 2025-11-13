// Api/Controller/MeProfileController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/me/profile")]

public sealed class MeProfileController : ControllerBase
{
    private readonly IUserProfileService _profiles;
    public MeProfileController(IUserProfileService profiles) => _profiles = profiles;

    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetMine(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var res = await _profiles.GetMineAsync(userId, ct);
        return res is null ? NotFound() : Ok(res);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> UpsertMine([FromBody] UserProfileUpsertRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var res = await _profiles.UpsertMineAsync(userId, req, ct);
        return Ok(res);
    }
}