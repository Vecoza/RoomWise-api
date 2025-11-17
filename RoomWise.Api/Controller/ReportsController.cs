using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/reports")]
// [Authorize(Roles = "Admin")] // if/when you configure roles
[Authorize] // for now, just require authenticated user
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;
    
    [HttpGet("reservations-summary")]
    public async Task<ActionResult<ReservationSummaryResponse>> ReservationsSummary(
        [FromQuery] int? hotelId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
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