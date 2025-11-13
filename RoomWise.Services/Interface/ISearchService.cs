using RoomWise.Model.Requests;
using RoomWise.Model.Responses;

namespace RoomWise.Services.Interface;

public interface ISearchService
{
    Task<PagedResult<HotelSearchItemResponse>> SearchHotelsAsync(HotelSearchRequest req, CancellationToken ct = default);
}