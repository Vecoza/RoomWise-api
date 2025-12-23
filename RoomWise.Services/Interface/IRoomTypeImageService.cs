using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IRoomTypeImageService : ICRUDService<RoomTypeImageResponse, RoomTypeImageSearchObject, RoomTypeImageUpsertRequest, RoomTypeImageUpsertRequest>
{
    Task ReorderAsync(RoomTypeImageReorderRequest req, CancellationToken ct = default);
    void ForceHotelScope(int hotelId);
}
