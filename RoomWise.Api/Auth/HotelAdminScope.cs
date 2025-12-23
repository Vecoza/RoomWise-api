using System.Security.Claims;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Auth;

public sealed class HotelAdminScope
{
    private readonly IHttpContextAccessor _http;
    private readonly IHotelAdminService _hotelAdmins;

    public HotelAdminScope(IHttpContextAccessor http, IHotelAdminService hotelAdmins)
    {
        _http = http;
        _hotelAdmins = hotelAdmins;
    }

    public async Task<int?> GetHotelIdAsync(CancellationToken ct = default)
    {
        var userId = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? _http.HttpContext?.User?.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return await _hotelAdmins.GetHotelIdForUserAsync(userId, ct);
    }
}
