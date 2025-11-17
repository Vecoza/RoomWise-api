using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IWishlistService
{
	Task<bool> AddAsync(string userId, int hotelId);
	Task<bool> RemoveAsync(string userId, int hotelId);
	Task<IReadOnlyList<HotelSearchItemResponse>> ListAsync(string userId);
}


