using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Services.Services;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class RoomTypesController :
    BaseCRUDController<RoomTypeResponse, RoomTypeSearchObject, RoomTypeUpsertRequest, RoomTypeUpsertRequest>
{
    public RoomTypesController(IRoomTypeService svc) : base(svc) { }
}