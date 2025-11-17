using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class ReportService : IReportService
{
    private readonly DbContext _db;

    public ReportService(DbContext db) => _db = db;

    public async Task<ReservationSummaryResponse> GetReservationSummaryAsync(
        ReservationReportFilter filter,
        CancellationToken ct = default)
    {
        var q = _db.Set<Reservation>().AsQueryable();

        if (filter.HotelId.HasValue)
            q = q.Where(r => r.HotelId == filter.HotelId.Value);

        if (filter.From.HasValue)
        {
            var fromDate = filter.From.Value.Date;
            q = q.Where(r => r.CheckIn.Date >= fromDate);
        }

        if (filter.To.HasValue)
        {
            var toDate = filter.To.Value.Date;
            q = q.Where(r => r.CheckOut.Date <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(r => r.Status == filter.Status);

        var list = await q.ToListAsync(ct);

        var summary = new ReservationSummaryResponse
        {
            TotalReservations = list.Count,
            TotalNights       = list.Sum(r => Math.Max(0, (r.CheckOut.Date - r.CheckIn.Date).Days)),
            TotalRevenue      = list.Sum(r => r.Subtotal)
        };

        summary.StatusBreakdown = list
            .GroupBy(r => r.Status)
            .Select(g => new ReservationStatusCount
            {
                Status = g.Key,
                Count  = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return summary;
    }
}