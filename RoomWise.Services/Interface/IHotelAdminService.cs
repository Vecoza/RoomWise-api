namespace RoomWise.Services.Interface;

public interface IHotelAdminService
{
    Task<int?> GetHotelIdForUserAsync(string userId, CancellationToken ct = default);
}
