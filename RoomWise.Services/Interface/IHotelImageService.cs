using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IHotelImageService
    : ICRUDService<HotelImageResponse, HotelImageSearchObject, HotelImageUpsertRequest, HotelImageUpsertRequest>
{
    Task ReorderAsync(HotelImageReorderRequest req, CancellationToken ct = default);
    Task<bool> ValidateHotelAsync(int hotelId, IList<int> imageIds, CancellationToken ct = default);
    void ForceHotelScope(int hotelId);
}
