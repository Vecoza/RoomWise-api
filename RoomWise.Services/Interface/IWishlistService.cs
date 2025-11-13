using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IWishlistService
{
	Task<bool> AddAsync(Guid userId, int hotelId);
	Task<bool> RemoveAsync(Guid userId, int hotelId);
	Task<IReadOnlyList<HotelSearchItemResponse>> ListAsync(Guid userId);
}


