using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;


[ApiController]
[Route("api/[controller]")]
public class HotelController : BaseCRUDController<HotelResponse, HotelSearchObject, HotelUpsertRequest, HotelUpsertRequest>
{
    public HotelController(IHotelService svc) : base(svc) { }
}