using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class StatisticsService : IStatisticsService
{
    private readonly DbContext _context;

    public StatisticsService(DbContext context) => _context = context;

    public async Task<AdminStatsOverviewResponse> GetOverviewAsync(CancellationToken ct = default)
    {
        var reservations = _context.Set<Reservation>().AsNoTracking();
        var payments     = _context.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == "Succeeded");
        var users        = _context.Set<AppUser>().AsNoTracking();
        var roomTypes    = _context.Set<RoomType>().AsNoTracking();

        var totalReservations = await reservations.CountAsync(ct);
        var totalRevenue      = await payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var totalUsers        = await users.CountAsync(ct);

        // Average stay length (in memory – fine for project scale)
        var stays = await reservations
            .Where(r => r.CheckOut > r.CheckIn)
            .Select(r => new { r.CheckIn, r.CheckOut })
            .ToListAsync(ct);

        double avgStay = 0;
        if (stays.Count > 0)
        {
            avgStay = stays
                .Average(s => (s.CheckOut.Date - s.CheckIn.Date).TotalDays);
        }

        // Occupancy for last 30 days
        var today = DateTime.UtcNow.Date;
        var from  = today.AddDays(-30);

        var roomTypesList = await roomTypes.ToListAsync(ct);
        var totalRoomNights = roomTypesList.Sum(rt => rt.Stock * 30);

        double usedRoomNights = 0;

        if (totalRoomNights > 0)
        {
            var relevantReservations = await reservations
                .Where(r =>
                    r.Status != "Cancelled" &&
                    r.CheckOut > from &&
                    r.CheckIn < today)
                .Select(r => new { r.CheckIn, r.CheckOut, r.RoomTypeId })
                .ToListAsync(ct);

            foreach (var r in relevantReservations)
            {
                var start = r.CheckIn.Date < from ? from : r.CheckIn.Date;
                var end   = r.CheckOut.Date > today ? today : r.CheckOut.Date;

                var nights = (end - start).Days;
                if (nights > 0)
                    usedRoomNights += nights;
            }
        }

        var occupancy = totalRoomNights == 0
            ? 0
            : usedRoomNights / totalRoomNights;

        return new AdminStatsOverviewResponse
        {
            TotalRevenue             = totalRevenue,
            TotalReservations        = totalReservations,
            TotalUsers               = totalUsers,
            AvgStayLengthNights      = avgStay,
            OccupancyRateLast30Days  = occupancy
        };
    }

    public async Task<IReadOnlyList<RevenueByMonthItem>> GetRevenueByMonthAsync(
        int year,
        CancellationToken ct = default)
    {
        if (year <= 0) year = DateTime.UtcNow.Year;

        var payments = _context.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == "Succeeded" && p.CreatedAt.Year == year);

        var grouped = await payments
            .GroupBy(p => p.CreatedAt.Month)
            .Select(g => new
            {
                Month   = g.Key,
                Revenue = g.Sum(p => p.Amount)
            })
            .ToListAsync(ct);

        var dict = grouped.ToDictionary(x => x.Month, x => x.Revenue);

        var result = new List<RevenueByMonthItem>(12);
        for (int m = 1; m <= 12; m++)
        {
            dict.TryGetValue(m, out var rev);
            result.Add(new RevenueByMonthItem
            {
                Month   = m,
                Revenue = rev
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<HotelStatsItem>> GetTopHotelsAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        limit = limit <= 0 ? 5 : limit;

        var fromDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var toDate   = to   ?? DateTime.UtcNow;

        var reservations = _context.Set<Reservation>().AsNoTracking()
            .Where(r =>
                r.CreatedAt >= fromDate &&
                r.CreatedAt <= toDate &&
                r.Status != "Cancelled");

        var payments = _context.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == "Succeeded");

        var hotels = _context.Set<Hotel>().AsNoTracking();

        // Pull to memory – simpler and OK for project scale
        var rows = await (
            from r in reservations
            join h in hotels on r.HotelId equals h.Id
            join p in payments on r.Id equals p.ReservationId into payGroup
            select new
            {
                h.Id,
                h.Name,
                h.Rating,
                ReservationId = r.Id,
                Revenue = payGroup.Sum(x => (decimal?)x.Amount) ?? 0m
            }).ToListAsync(ct);

        var grouped = rows
            .GroupBy(x => new { x.Id, x.Name, x.Rating })
            .Select(g => new HotelStatsItem
            {
                HotelId          = g.Key.Id,
                HotelName        = g.Key.Name,
                Rating           = (double)g.Key.Rating,
                ReservationsCount = g.Select(x => x.ReservationId).Distinct().Count(),
                Revenue          = g.Sum(x => x.Revenue)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.ReservationsCount)
            .Take(limit)
            .ToList();

        return grouped;
    }

    public async Task<IReadOnlyList<UserStatsItem>> GetTopUsersAsync(
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        limit = limit <= 0 ? 5 : limit;

        var fromDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var toDate   = to   ?? DateTime.UtcNow;

        var reservations = _context.Set<Reservation>().AsNoTracking()
            .Where(r =>
                r.CreatedAt >= fromDate &&
                r.CreatedAt <= toDate &&
                r.Status != "Cancelled");

        var payments = _context.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == "Succeeded");

        var users = _context.Set<AppUser>().AsNoTracking();

        var rows = await (
            from r in reservations
            join u in users on r.UserId equals u.Id
            join p in payments on r.Id equals p.ReservationId into payGroup
            select new
            {
                u.Id,
                u.Email,
                FullName = (u.UserName ?? u.Email),
                ReservationId = r.Id,
                r.CheckIn,
                r.CheckOut,
                Revenue = payGroup.Sum(x => (decimal?)x.Amount) ?? 0m
            }).ToListAsync(ct);

        var grouped = rows
            .GroupBy(x => new { x.Id, x.Email, x.FullName })
            .Select(g =>
            {
                var reservationsList = g.ToList();
                var nights = reservationsList.Sum(r =>
                    Math.Max(0, (r.CheckOut.Date - r.CheckIn.Date).Days));

                return new UserStatsItem
                {
                    UserId            = g.Key.Id,
                    Email             = g.Key.Email,
                    FullName          = g.Key.FullName,
                    ReservationsCount = reservationsList
                        .Select(r => r.ReservationId).Distinct().Count(),
                    Revenue           = g.Sum(r => r.Revenue),
                    Nights            = nights
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Nights)
            .Take(limit)
            .ToList();

        return grouped;
    }
}
