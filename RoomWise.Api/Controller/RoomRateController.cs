using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class RoomRatesController : BaseCRUDController<RoomRateResponse, RoomRateSearchObject, RoomRateRequest, RoomRateRequest>
{
    public RoomRatesController(IRoomRateService service) : base(service) { }
}