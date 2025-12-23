using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using RoomWise.Model;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class RoomRatesController : BaseCRUDController<RoomRateResponse, RoomRateSearchObject, RoomRateRequest, RoomRateRequest>
{
    private readonly IRoomRateService _svc;
    private readonly HotelAdminScope _scope;

    public RoomRatesController(IRoomRateService service, HotelAdminScope scope) : base(service)
    {
        _svc = service;
        _scope = scope;
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PagedResult<RoomRateResponse>> Get([FromQuery] RoomRateSearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomRateResponse> Create([FromBody] RoomRateRequest req)
    {
        return Filtered(async () => await base.Create(req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomRateResponse?> Update(int id, [FromBody] RoomRateRequest req)
    {
        return Filtered(async () => await base.Update(id, req));
    }

    private async Task<T> Filtered<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue)
        {
            _svc.ForceHotelScope(hotelId.Value);
        }
        return await action();
    }
}
