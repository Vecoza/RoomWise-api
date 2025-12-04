using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class CitiesController
    : BaseCRUDController<CityResponse, CitySearchObject, CityUpsertRequest, CityUpsertRequest>
{
    public CitiesController(ICityService service) : base(service) { }
}
