using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IRoomRateService
    : ICRUDService<RoomRateResponse, RoomRateSearchObject, RoomRateRequest, RoomRateRequest>
{
    void ForceHotelScope(int hotelId);
}
