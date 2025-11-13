// RoomWise.Services/Services/TagService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class TagService
    : BaseCRUDService<TagResponse, TagSearchObject, Tag, TagUpsertRequest, TagUpsertRequest>, ITagService
{
    private readonly DbContext _db;
    public TagService(DbContext db, IMapper mapper) : base(db, mapper) => _db = db;

    protected override IQueryable<Tag> ApplyFilter(IQueryable<Tag> q, TagSearchObject s)
    {
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(t => EF.Functions.ILike(t.Name, $"%{s.FTS}%"));
        if (!string.IsNullOrWhiteSpace(s.Name))
            q = q.Where(t => EF.Functions.ILike(t.Name, $"%{s.Name}%"));
        return q.OrderBy(t => t.Name);
    }

    public async Task SetForHotelAsync(int hotelId, IEnumerable<int> tagIds, CancellationToken ct = default)
    {
        var set = _db.Set<HotelTag>();
        var existing = await set.Where(x => x.HotelId == hotelId).ToListAsync(ct);

        var toDelete = existing.Where(e => !tagIds.Contains(e.TagId)).ToList();
        if (toDelete.Count > 0) set.RemoveRange(toDelete);

        var existingIds = existing.Select(e => e.TagId).ToHashSet();
        var toAdd = tagIds.Where(id => !existingIds.Contains(id))
            .Select(id => new HotelTag { HotelId = hotelId, TagId = id });
        await set.AddRangeAsync(toAdd, ct);

        await _db.SaveChangesAsync(ct);
    }
}