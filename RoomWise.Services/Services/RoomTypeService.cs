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
    public RoomTypeService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<RoomType> ApplyFilter(IQueryable<RoomType> q, RoomTypeSearchObject s)
    {
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
        if (entity.BasePrice < 0)        throw new ArgumentException("BasePrice cannot be negative.");
        if (entity.Stock < 0)            throw new ArgumentException("Stock cannot be negative.");
        if (entity.Capacity < 1)         throw new ArgumentException("Capacity must be >= 1.");

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
        if (entity.Stock < 0)     throw new ArgumentException("Stock cannot be negative.");
        if (entity.Capacity < 1)  throw new ArgumentException("Capacity must be >= 1.");

        return Task.CompletedTask;
    }
}
