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
public sealed class AddOnsController
    : BaseCRUDController<AddOnResponse, AddOnSearchObject, AddOnUpsertRequest, AddOnUpsertRequest>
{
    private readonly IAddOnService _svc;
    private readonly HotelAdminScope _scope;

    public AddOnsController(IAddOnService svc, HotelAdminScope scope) : base(svc)
    {
        _svc = svc;
        _scope = scope;
    }

    [AllowAnonymous]
    public override Task<PagedResult<AddOnResponse>> Get([FromQuery] AddOnSearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<AddOnResponse> Create([FromBody] AddOnUpsertRequest req)
    {
        return Filtered(async () => await base.Create(req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<AddOnResponse?> Update(int id, [FromBody] AddOnUpsertRequest req)
    {
        return Filtered(async () => await base.Update(id, req));
    }

    private async Task<T> Filtered<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue) _svc.ForceHotelScope(hotelId.Value);
        return await action();
    }
}
