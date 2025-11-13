
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class RoomAvailabilityService
    : BaseCRUDService<RoomAvailabilityResponse, RoomAvailabilitySearchObject, RoomAvailability, RoomAvailabilityUpsertRequest, RoomAvailabilityUpsertRequest>,
      IRoomAvailabilityService
{
    public RoomAvailabilityService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<RoomAvailability> ApplyFilter(IQueryable<RoomAvailability> q, RoomAvailabilitySearchObject s)
    {
        if (s.RoomTypeId.HasValue) q = q.Where(x => x.RoomTypeId == s.RoomTypeId.Value);
        if (s.From.HasValue)       q = q.Where(x => x.Date >= s.From.Value.Date);
        if (s.To.HasValue)         q = q.Where(x => x.Date <  s.To.Value.Date);
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.RoomTypeId.ToString() == s.FTS); 
        return q.OrderBy(x => x.RoomTypeId).ThenBy(x => x.Date);
    }

    protected override Task BeforeInsert(RoomAvailability entity, RoomAvailabilityUpsertRequest request)
    {
        if (request.Available < 0) throw new ArgumentException("Available cannot be negative.");
        entity.Date = request.Date.Date;
        return Task.CompletedTask;
    }

    protected override Task BeforeUpdate(RoomAvailability entity, RoomAvailabilityUpsertRequest request)
    {
        if (request.Available < 0) throw new ArgumentException("Available cannot be negative.");
        entity.Date = request.Date.Date;
        return Task.CompletedTask;
    }

    public async Task BatchUpsertAsync(RoomAvailabilityBatchUpsertRequest req, CancellationToken ct = default)
    {
        if (req.Items is { Count: > 0 })
        {
            foreach (var item in req.Items)
            {
                if (item.Available < 0) throw new ArgumentException("Available cannot be negative.");
                await UpsertSingleAsync(item.RoomTypeId, item.Date.Date, item.Available, ct);
            }
            await _context.SaveChangesAsync(ct);
            return;
        }

        if (req.From is null || req.To is null || req.Available is null)
            throw new ArgumentException("Provide either Items[] or From/To/Available.");

        var from = req.From.Value.Date;
        var to   = req.To.Value.Date;
        if (to <= from) throw new ArgumentException("To must be after From.");
        if (req.Available!.Value < 0) throw new ArgumentException("Available cannot be negative.");

        for (var d = from; d < to; d = d.AddDays(1))
            await UpsertSingleAsync(req.RoomTypeId, d, req.Available.Value, ct);

        await _context.SaveChangesAsync(ct);
    }

    private async Task UpsertSingleAsync(int roomTypeId, DateTime date, int available, CancellationToken ct)
    {
        var existing = await _context.Set<RoomAvailability>()
            .FirstOrDefaultAsync(x => x.RoomTypeId == roomTypeId && x.Date == date, ct);

        if (existing is null)
        {
            _context.Set<RoomAvailability>().Add(new RoomAvailability
            {
                RoomTypeId = roomTypeId,
                Date = date,
                Available = available
            });
        }
        else
        {
            existing.Available = available;
        }
    }


    public async Task EnsureRangeConfiguredAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var from = checkIn.Date;
        var to   = checkOut.Date;
        var nights = (to - from).Days;
        if (nights <= 0) throw new ArgumentException("Invalid stay range.");

        var count = await _context.Set<RoomAvailability>()
            .Where(a => a.RoomTypeId == roomTypeId && a.Date >= from && a.Date < to)
            .CountAsync(ct);

        if (count != nights)
            throw new InvalidOperationException("Availability not configured for all dates in the selected range.");
    }

    public async Task<bool> TryConsumeRangeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var from = checkIn.Date;
        var to   = checkOut.Date;

        var records = await _context.Set<RoomAvailability>()
            .Where(a => a.RoomTypeId == roomTypeId && a.Date >= from && a.Date < to)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);

       
        var nights = (to - from).Days;
        if (records.Count != nights) return false;
        if (records.Any(a => a.Available <= 0)) return false;

  
        foreach (var a in records)
            a.Available -= 1;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task RestoreRangeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var from = checkIn.Date;
        var to   = checkOut.Date;

        var records = await _context.Set<RoomAvailability>()
            .Where(a => a.RoomTypeId == roomTypeId && a.Date >= from && a.Date < to)
            .ToListAsync(ct);

        foreach (var a in records)
            a.Available += 1;

        await _context.SaveChangesAsync(ct);
    }
}
