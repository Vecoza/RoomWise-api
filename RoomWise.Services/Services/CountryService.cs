using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class CountryService
    : BaseCRUDService<CountryResponse, CountrySearchObject, Country, CountryUpsertRequest, CountryUpsertRequest>,
      ICountryService
{
    public CountryService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<Country> ApplyFilter(IQueryable<Country> q, CountrySearchObject s)
    {
        if (!string.IsNullOrWhiteSpace(s.Name))
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{s.Name}%"));

        if (!string.IsNullOrWhiteSpace(s.Iso2))
            q = q.Where(c => c.Iso2 != null && EF.Functions.ILike(c.Iso2, $"%{s.Iso2}%"));

        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(c =>
                EF.Functions.ILike(c.Name, $"%{s.FTS}%") ||
                (c.Iso2 != null && EF.Functions.ILike(c.Iso2, $"%{s.FTS}%")));

        return q.OrderBy(c => c.Name);
    }
}
