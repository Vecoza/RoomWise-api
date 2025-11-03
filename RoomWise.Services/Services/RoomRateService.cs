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
    public RoomRateService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<RoomRate> ApplyFilter(IQueryable<RoomRate> q, RoomRateSearchObject s)
    {
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

    // Override BeforeInsert/BeforeUpdate to validate date ranges and overlap server-side
    protected override async Task BeforeInsert(RoomRate entity, RoomRateRequest request)
    {
        // Check EndDate >= StartDate
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("EndDate must be >= StartDate");

        // Check overlap for same RoomType
        var exists = await _context.Set<RoomRate>()
            .AnyAsync(r =>
                r.RoomTypeId == request.RoomTypeId &&
                !(r.EndDate < request.StartDate || r.StartDate > request.EndDate));
        if (exists)
            throw new InvalidOperationException("Overlapping rate exists for this room type.");
    }

    protected override async Task BeforeUpdate(RoomRate entity, RoomRateRequest request)
    {
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("EndDate must be >= StartDate");

        var exists = await _context.Set<RoomRate>()
            .AnyAsync(r =>
                r.Id != entity.Id &&
                r.RoomTypeId == request.RoomTypeId &&
                !(r.EndDate < request.StartDate || r.StartDate > request.EndDate));
        if (exists)
            throw new InvalidOperationException("Overlapping rate exists for this room type.");
    }
}