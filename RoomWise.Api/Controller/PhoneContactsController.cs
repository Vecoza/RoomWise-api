using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class PhoneContactsController
    : BaseCRUDController<PhoneContactResponse, PhoneContactSearchObject, PhoneContactUpsertRequest, PhoneContactUpsertRequest>
{
    public PhoneContactsController(IPhoneContactService svc) : base(svc) { }
}