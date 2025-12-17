using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recs;

    public RecommendationsController(IRecommendationService recs)
    {
        _recs = recs;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int top = 10, CancellationToken ct = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var items = await _recs.GetForUserAsync(userId, top, ct);
        return Ok(new { items });
    }
}
