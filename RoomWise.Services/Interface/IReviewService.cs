using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;



public interface IReviewService
    : ICRUDService<ReviewResponse, ReviewSearchObject, ReviewUpsertRequest, ReviewUpsertRequest>
{

    Task<ReviewResponse> CreateAsync(ReviewUpsertRequest req, CancellationToken ct = default);

    Task<PagedResult<ReviewResponse>> ListByHotelAsync(
        int hotelId, int page = 0, int pageSize = 10, CancellationToken ct = default);

    void ForceHotelScope(int hotelId);
}
