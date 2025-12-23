using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = AppRoles.Administrator)]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly HotelAdminScope _scope;

    public ReportsController(IReportService reports, HotelAdminScope scope)
    {
        _reports = reports;
        _scope = scope;
    }
    
    [HttpGet("reservations-summary")]
    public async Task<ActionResult<ReservationSummaryResponse>> ReservationsSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (!hotelId.HasValue) return Forbid();

        var filter = new ReservationReportFilter
        {
            HotelId = hotelId,
            From    = from,
            To      = to,
            Status  = status
        };

        var result = await _reports.GetReservationSummaryAsync(filter, ct);
        return Ok(result);
    }
}
