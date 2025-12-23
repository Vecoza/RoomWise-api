using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IRoomAvailabilityService
    : ICRUDService<RoomAvailabilityResponse, RoomAvailabilitySearchObject, RoomAvailabilityUpsertRequest, RoomAvailabilityUpsertRequest>
{
    Task BatchUpsertAsync(RoomAvailabilityBatchUpsertRequest req, CancellationToken ct = default);


    Task EnsureRangeConfiguredAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    Task<bool> TryConsumeRangeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    Task RestoreRangeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    void ForceHotelScope(int hotelId);
}
