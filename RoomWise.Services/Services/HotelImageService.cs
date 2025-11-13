
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class HotelImageService
    : BaseCRUDService<HotelImageResponse, HotelImageSearchObject, HotelImage, HotelImageUpsertRequest, HotelImageUpsertRequest>, IHotelImageService
{
    private readonly DbContext _db;
    public HotelImageService(DbContext db, IMapper mapper) : base(db, mapper) => _db = db;

    protected override IQueryable<HotelImage> ApplyFilter(IQueryable<HotelImage> q, HotelImageSearchObject s)
    {
        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        return q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id);
    }

    public async Task ReorderAsync(HotelImageReorderRequest req, CancellationToken ct = default)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var entities = await _db.Set<HotelImage>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);

        foreach (var (id, order) in req.Items)
        {
            var e = entities.First(x => x.Id == id);
            e.SortOrder = order;
        }
        await _db.SaveChangesAsync(ct);
    }
}