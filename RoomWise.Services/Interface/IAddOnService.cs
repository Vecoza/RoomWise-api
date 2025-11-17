using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;


public interface IAddOnService
    : ICRUDService<AddOnResponse, AddOnSearchObject, AddOnUpsertRequest, AddOnUpsertRequest>
{
}