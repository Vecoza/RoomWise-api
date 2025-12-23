using Microsoft.EntityFrameworkCore;
using RoomWise.Services.Interface;
using RoomWise.Model;

namespace RoomWise.Services.Services;

public sealed class HotelAdminService : IHotelAdminService
{
    private readonly DbContext _db;

    public HotelAdminService(DbContext db)
    {
        _db = db;
    }

    public async Task<int?> GetHotelIdForUserAsync(string userId, CancellationToken ct = default)
    {
        var ha = await _db.Set<HotelAdmin>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        return ha?.HotelId;
    }
}
