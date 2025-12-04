using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class FacilityService
    : BaseCRUDService<FacilityResponse, FacilitySearchObject, Facility, FacilityUpsertRequest, FacilityUpsertRequest>,
      IFacilityService
{
    public FacilityService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<Facility> ApplyFilter(IQueryable<Facility> q, FacilitySearchObject s)
    {
        if (!string.IsNullOrWhiteSpace(s.Code))
            q = q.Where(f => EF.Functions.ILike(f.Code, $"%{s.Code}%"));

        if (!string.IsNullOrWhiteSpace(s.Name))
            q = q.Where(f => EF.Functions.ILike(f.Name, $"%{s.Name}%"));

        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(f => EF.Functions.ILike(f.Name, $"%{s.FTS}%") ||
                             EF.Functions.ILike(f.Code, $"%{s.FTS}%"));

        return q.OrderBy(f => f.Name);
    }
}
