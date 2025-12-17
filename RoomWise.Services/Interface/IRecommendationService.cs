using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface IRecommendationService
{
    Task<IReadOnlyList<HotelSearchItemResponse>> GetForUserAsync(
        string userId,
        int top = 10,
        CancellationToken ct = default);
}
