using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/admin/stats")]
/*[Authorize(Roles = AppRoles.Administrator)]*/
public sealed class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _stats;
    private readonly HotelAdminScope _scope;

    public StatisticsController(IStatisticsService stats, HotelAdminScope scope)
    {
        _stats = stats;
        _scope = scope;
    } 


    [HttpGet("overview")]
    public async Task<AdminStatsOverviewResponse> Overview(CancellationToken ct)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        return await _stats.GetOverviewAsync(hotelId, ct);
    }

   
    [HttpGet("revenue-by-month")]
    public async Task<IReadOnlyList<RevenueByMonthItem>> RevenueByMonth(
        [FromQuery] int year,
        CancellationToken ct)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        return await _stats.GetRevenueByMonthAsync(year, hotelId, ct);
    }

   
    [HttpGet("top-hotels")]
    public async Task<IReadOnlyList<HotelStatsItem>> TopHotels(
        [FromQuery] int limit = 5,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        return await _stats.GetTopHotelsAsync(limit, from, to, hotelId, ct);
    }

    
    [HttpGet("top-users")]
    public async Task<IReadOnlyList<UserStatsItem>> TopUsers(
        [FromQuery] int limit = 5,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        return await _stats.GetTopUsersAsync(limit, from, to, hotelId, ct);
    }
}
