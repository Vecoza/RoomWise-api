using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;


namespace RoomWise.Services.Services;

public sealed class HotelService
    : BaseCRUDService<HotelResponse, HotelSearchObject, Hotel, HotelUpsertRequest, HotelUpsertRequest>,
        IHotelService
{
    public HotelService(DbContext context, IMapper mapper) : base(context, mapper) { }

    protected override IQueryable<Hotel> ApplyFilter(IQueryable<Hotel> q, HotelSearchObject s)
    {
        if (s.CityId.HasValue) q = q.Where(x => x.CityId == s.CityId.Value);
        if (!string.IsNullOrWhiteSpace(s.Name)) q = q.Where(x => x.Name.Contains(s.Name));
        if (s.MinRating.HasValue) q = q.Where(x => x.Rating >= s.MinRating.Value);
        if (s.MaxRating.HasValue) q = q.Where(x => x.Rating <= s.MaxRating.Value);
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.Name.Contains(s.FTS!) || x.Description.Contains(s.FTS!));
        return q.OrderByDescending(x => x.Rating);
    }
}