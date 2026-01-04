using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;


public sealed class RoomTypeService
  : BaseCRUDService<RoomTypeResponse, RoomTypeSearchObject, RoomType, RoomTypeUpsertRequest, RoomTypeUpsertRequest>,
    IRoomTypeService
{
    private int? _forcedHotelId;

    public RoomTypeService(DbContext context, IMapper mapper) : base(context, mapper) { }

    public void ForceHotelScope(int hotelId) => _forcedHotelId = hotelId;

    protected override IQueryable<RoomType> ApplyFilter(IQueryable<RoomType> q, RoomTypeSearchObject s)
    {
        if (_forcedHotelId.HasValue)
        {
            s.HotelId = _forcedHotelId.Value;
        }

        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        if (!string.IsNullOrWhiteSpace(s.Name)) q = q.Where(x => x.Name.Contains(s.Name));
        if (!string.IsNullOrWhiteSpace(s.BedType)) q = q.Where(x => x.BedType == s.BedType);
        if (s.MinCapacity.HasValue) q = q.Where(x => x.Capacity >= s.MinCapacity.Value);
        if (s.MaxCapacity.HasValue) q = q.Where(x => x.Capacity <= s.MaxCapacity.Value);
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.Name.Contains(s.FTS!) || x.BedType.Contains(s.FTS!));
        return q.OrderBy(x => x.Name);
    }

    protected override Task BeforeInsert(RoomType entity, RoomTypeUpsertRequest req)
    {
        if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;

        entity.Currency = string.IsNullOrWhiteSpace(req.Currency)
           ? "EUR"
           : req.Currency!.Trim().ToUpperInvariant();

        if (entity.Currency.Length != 3) throw new ArgumentException("Currency must be 3 letters.");
        if (entity.BasePrice < 0) throw new ArgumentException("BasePrice cannot be negative.");
        if (entity.Stock < 0) throw new ArgumentException("Stock cannot be negative.");
        if (entity.Capacity < 1) throw new ArgumentException("Capacity must be >= 1.");

        return Task.CompletedTask;
    }

    protected override Task BeforeUpdate(RoomType entity, RoomTypeUpsertRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Currency))
        {
            entity.Currency = req.Currency!.Trim().ToUpperInvariant();
            if (entity.Currency.Length != 3) throw new ArgumentException("Currency must be 3 letters.");
        }

        if (entity.BasePrice < 0) throw new ArgumentException("BasePrice cannot be negative.");
        if (entity.Stock < 0) throw new ArgumentException("Stock cannot be negative.");
        if (entity.Capacity < 1) throw new ArgumentException("Capacity must be >= 1.");

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RoomTypeAvailabilityResponse>> GetAvailabilityAsync(DateTime date, CancellationToken ct)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var roomTypes = _context.Set<RoomType>()
            .AsNoTracking();

        if (_forcedHotelId.HasValue)
            roomTypes = roomTypes.Where(rt => rt.HotelId == _forcedHotelId.Value);

        var types = await roomTypes
            .Select(rt => new
            {
                rt.Id,
                rt.Name,
                rt.Stock,
                rt.Currency,
                rt.HotelId
            })
            .ToListAsync(ct);

        if (types.Count == 0)
            return Array.Empty<RoomTypeAvailabilityResponse>();

        var roomTypeIds = types.Select(t => t.Id).ToList();
        var activeStatuses = new[] { "Pending", "Confirmed" };

        var reservations = _context.Set<Reservation>()
            .AsNoTracking()
            .Where(r => roomTypeIds.Contains(r.RoomTypeId))
            .Where(r => r.CheckIn < dayEnd && r.CheckOut > dayStart)
            .Where(r => activeStatuses.Contains(r.Status));

        if (_forcedHotelId.HasValue)
            reservations = reservations.Where(r => r.HotelId == _forcedHotelId.Value);

        var reservedByRoomType = await reservations
            .GroupBy(r => r.RoomTypeId)
            .Select(g => new { RoomTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomTypeId, x => x.Count, ct);

        var result = new List<RoomTypeAvailabilityResponse>();
        foreach (var t in types.OrderBy(t => t.Name))
        {
            reservedByRoomType.TryGetValue(t.Id, out var reserved);
            var available = Math.Max(0, t.Stock - reserved);

            result.Add(new RoomTypeAvailabilityResponse
            {
                RoomTypeId = t.Id,
                RoomTypeName = t.Name,
                Stock = t.Stock,
                Reserved = reserved,
                Available = available,
                Currency = t.Currency,
                Date = dayStart
            });
        }

        return result;
    }
}
