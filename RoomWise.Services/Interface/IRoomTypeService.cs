using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Services;

namespace RoomWise.Services.Interface;

public interface IRoomTypeService : ICRUDService<RoomTypeResponse, RoomTypeSearchObject, RoomTypeUpsertRequest, RoomTypeUpsertRequest>

{
    void ForceHotelScope(int hotelId);
    Task<IReadOnlyList<RoomTypeAvailabilityResponse>> GetAvailabilityAsync(DateTime date, CancellationToken ct);
}
