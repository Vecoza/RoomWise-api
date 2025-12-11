using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class AddOnService
    : BaseCRUDService<AddOnResponse, AddOnSearchObject, AddOn, AddOnUpsertRequest, AddOnUpsertRequest>,
      IAddOnService
{
    public AddOnService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<AddOn> ApplyFilter(IQueryable<AddOn> q, AddOnSearchObject s)
    {
        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        if (s.IsActive.HasValue) q = q.Where(x => x.IsActive == s.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.Name.Contains(s.FTS!) || (x.Description != null && x.Description.Contains(s.FTS!)));

        return q.OrderBy(x => x.HotelId).ThenBy(x => x.Name);
    }

    protected override Task BeforeInsert(AddOn entity, AddOnUpsertRequest req)
        => ValidateAndNormalize(entity, req);

    protected override Task BeforeUpdate(AddOn entity, AddOnUpsertRequest req)
        => ValidateAndNormalize(entity, req);

    private static Task ValidateAndNormalize(AddOn entity, AddOnUpsertRequest req)
    {
        if (req.Price < 0) throw new ArgumentException("Price cannot be negative.");

        var model = (req.PricingModel ?? "").Trim();
        if (!string.Equals(model, "PerNight", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model, "PerStay", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model, "PerGuestPerNight", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("PricingModel must be 'PerNight', 'PerStay' or 'PerGuestPerNight'.");
        entity.PricingModel = model;
        entity.Currency = string.IsNullOrWhiteSpace(req.Currency)
            ? "EUR"
            : req.Currency.Trim().ToUpperInvariant();

        if (entity.Currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.");

        return Task.CompletedTask;
    }
}
