using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public sealed class RoomTypeImageService
    : BaseCRUDService<RoomTypeImageResponse, RoomTypeImageSearchObject, RoomTypeImage, RoomTypeImageUpsertRequest, RoomTypeImageUpsertRequest>,
      IRoomTypeImageService
{
    private readonly DbContext _db;
    private int? _forcedHotelId;

    public RoomTypeImageService(DbContext db, IMapper mapper) : base(db, mapper) => _db = db;

    public void ForceHotelScope(int hotelId) => _forcedHotelId = hotelId;

    protected override IQueryable<RoomTypeImage> ApplyFilter(IQueryable<RoomTypeImage> q, RoomTypeImageSearchObject s)
    {
        if (_forcedHotelId.HasValue)
        {
            var rtIds = _db.Set<RoomType>().Where(rt => rt.HotelId == _forcedHotelId.Value).Select(rt => rt.Id);
            q = q.Where(x => rtIds.Contains(x.RoomTypeId));
        }

        if (s.HotelId.HasValue)
        {
            var rtIds = _db.Set<RoomType>().Where(rt => rt.HotelId == s.HotelId.Value).Select(rt => rt.Id);
            q = q.Where(x => rtIds.Contains(x.RoomTypeId));
        }

        if (s.RoomTypeId.HasValue) q = q.Where(x => x.RoomTypeId == s.RoomTypeId.Value);

        return q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id);
    }

    public async Task ReorderAsync(RoomTypeImageReorderRequest req, CancellationToken ct = default)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var entities = await _db.Set<RoomTypeImage>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);

        var missing = ids.Except(entities.Select(e => e.Id)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"Room type image id(s) not found: {string.Join(", ", missing)}");

        if (_forcedHotelId.HasValue)
        {
            var allowedRoomTypeIds = await _db.Set<RoomType>()
                .Where(rt => rt.HotelId == _forcedHotelId.Value)
                .Select(rt => rt.Id)
                .ToListAsync(ct);

            if (entities.Any(e => !allowedRoomTypeIds.Contains(e.RoomTypeId)))
                throw new UnauthorizedAccessException("Images do not belong to your hotel.");
        }

        foreach (var item in req.Items)
        {
            var e = entities.First(x => x.Id == item.Id);
            e.SortOrder = item.SortOrder;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ValidateRoomTypeAsync(int hotelId, int roomTypeId, CancellationToken ct = default)
    {
        return await _db.Set<RoomType>()
            .AnyAsync(rt => rt.Id == roomTypeId && rt.HotelId == hotelId, ct);
    }
}
