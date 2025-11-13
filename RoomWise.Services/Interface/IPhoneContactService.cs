using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Interface;

public interface IPhoneContactService
    : ICRUDService<PhoneContactResponse, PhoneContactSearchObject, PhoneContactUpsertRequest, PhoneContactUpsertRequest>
{ }