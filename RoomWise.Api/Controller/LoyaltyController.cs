// Api/Controller/LoyaltyController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyalty;
    public LoyaltyController(ILoyaltyService loyalty) => _loyalty = loyalty;

    [HttpGet("balance")]
    public async Task<ActionResult<LoyaltyBalanceResponse>> Balance(CancellationToken ct)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(uid)) return Forbid();

        var bal = await _loyalty.GetBalanceAsync(uid, ct);
        return Ok(new LoyaltyBalanceResponse { UserId = uid, Balance = bal });
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<LoyaltyPointResponse>>> History([FromQuery] int page = 0, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(uid)) return Forbid();

        var res = await _loyalty.GetHistoryAsync(uid, page, pageSize, ct);
        return Ok(res);
    }
}