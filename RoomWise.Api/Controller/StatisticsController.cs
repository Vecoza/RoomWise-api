using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/admin/stats")]
/*[Authorize(Roles = AppRoles.Administrator)]*/
public sealed class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _stats;

    public StatisticsController(IStatisticsService stats)
    {
        _stats = stats;
    } 


    [HttpGet("overview")]
    public Task<AdminStatsOverviewResponse> Overview(CancellationToken ct)
        => _stats.GetOverviewAsync(ct);

   
    [HttpGet("revenue-by-month")]
    public Task<IReadOnlyList<RevenueByMonthItem>> RevenueByMonth(
        [FromQuery] int year,
        CancellationToken ct)
        => _stats.GetRevenueByMonthAsync(year, ct);

    
    [HttpGet("top-hotels")]
    public Task<IReadOnlyList<HotelStatsItem>> TopHotels(
        [FromQuery] int limit = 5,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => _stats.GetTopHotelsAsync(limit, from, to, ct);

    
    [HttpGet("top-users")]
    public Task<IReadOnlyList<UserStatsItem>> TopUsers(
        [FromQuery] int limit = 5,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
        => _stats.GetTopUsersAsync(limit, from, to, ct);
}