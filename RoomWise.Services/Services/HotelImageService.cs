
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
    private int? _forcedHotelId;
    public HotelImageService(DbContext db, IMapper mapper) : base(db, mapper) => _db = db;

    public void ForceHotelScope(int hotelId) => _forcedHotelId = hotelId;

    protected override IQueryable<HotelImage> ApplyFilter(IQueryable<HotelImage> q, HotelImageSearchObject s)
    {
        if (_forcedHotelId.HasValue)
        {
            s.HotelId = _forcedHotelId.Value;
        }

        if (s.HotelId.HasValue) q = q.Where(x => x.HotelId == s.HotelId.Value);
        return q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id);
    }

    public async Task ReorderAsync(HotelImageReorderRequest req, CancellationToken ct = default)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var entities = await _db.Set<HotelImage>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);


        var missing = ids.Except(entities.Select(e => e.Id)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"Hotel image id(s) not found: {string.Join(", ", missing)}");

        foreach (var item in req.Items)
        {
            var e = entities.First(x => x.Id == item.Id);
            e.SortOrder = item.SortOrder;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ValidateHotelAsync(int hotelId, IList<int> imageIds, CancellationToken ct = default)
    {
        var count = await _db.Set<HotelImage>()
            .Where(i => imageIds.Contains(i.Id) && i.HotelId == hotelId)
            .CountAsync(ct);
        return count == imageIds.Count;
    }
}
