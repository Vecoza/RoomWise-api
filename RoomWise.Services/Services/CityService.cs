using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class CityService
    : BaseCRUDService<CityResponse, CitySearchObject, City, CityUpsertRequest, CityUpsertRequest>,
      ICityService
{
    public CityService(DbContext context, IMapper mapper) : base(context, mapper) { }


    protected override IQueryable<City> ApplyFilter(IQueryable<City> q, CitySearchObject s)
    {

        q = q.Include(c => c.Country);

        if (s.CountryId.HasValue)
            q = q.Where(c => c.CountryId == s.CountryId.Value);

        if (!string.IsNullOrWhiteSpace(s.Name))
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{s.Name}%"));

        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{s.FTS}%"));

        return q.OrderBy(c => c.Name);
    }


}
