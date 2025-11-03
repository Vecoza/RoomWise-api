using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IHotelService
    : ICRUDService<HotelResponse, HotelSearchObject, HotelUpsertRequest, HotelUpsertRequest>
{ }