using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class RoomRateService
    : BaseCRUDService<RoomRateResponse, RoomRateSearchObject, RoomRate, RoomRateRequest, RoomRateRequest>,
      IRoomRateService
{
    private int? _forcedHotelId;

    public RoomRateService(DbContext context, IMapper mapper) : base(context, mapper) { }

    public void ForceHotelScope(int hotelId) => _forcedHotelId = hotelId;

    protected override IQueryable<RoomRate> ApplyFilter(IQueryable<RoomRate> q, RoomRateSearchObject s)
    {
        if (_forcedHotelId.HasValue)
        {
            s.HotelId = _forcedHotelId.Value;
        }

        if (s.HotelId.HasValue)
        {
            var roomTypeIds = _context.Set<RoomType>()
                .Where(rt => rt.HotelId == s.HotelId.Value)
                .Select(rt => rt.Id);
            q = q.Where(x => roomTypeIds.Contains(x.RoomTypeId));
        }

        if (s.RoomTypeId.HasValue) q = q.Where(x => x.RoomTypeId == s.RoomTypeId.Value);
        if (s.Date.HasValue)
        {
            var d = s.Date.Value;
            q = q.Where(x => x.StartDate <= d && x.EndDate >= d);
        }
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.Currency.Contains(s.FTS!));
        return q.OrderBy(x => x.StartDate);
    }

    protected override Task BeforeInsert(RoomRate entity, RoomRateRequest req)
    {
        if (req.EndDate < req.StartDate)
            throw new ArgumentException("EndDate must be >= StartDate");

        if (req.Price < 0)
            throw new ArgumentException("Price cannot be negative.");

        entity.Currency = string.IsNullOrWhiteSpace(req.Currency)
            ? "EUR"
            : req.Currency!.Trim().ToUpperInvariant();

        if (entity.Currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter code.");

        return base.BeforeInsert(entity, req);
    }

    protected override async Task BeforeUpdate(RoomRate entity, RoomRateRequest req)
    {
        if (req.EndDate < req.StartDate)
            throw new ArgumentException("EndDate must be >= StartDate");

        if (req.Price < 0)
            throw new ArgumentException("Price cannot be negative.");

        if (!string.IsNullOrWhiteSpace(req.Currency))
        {
            var c = req.Currency.Trim().ToUpperInvariant();
            if (c.Length != 3) throw new ArgumentException("Currency must be a 3-letter code.");
            entity.Currency = c;
        }

        await base.BeforeUpdate(entity, req);
    }
}
