using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Services.Services;
using Microsoft.AspNetCore.Authorization;
using RoomWise.Model;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class RoomTypesController :
    BaseCRUDController<RoomTypeResponse, RoomTypeSearchObject, RoomTypeUpsertRequest, RoomTypeUpsertRequest>
{
    private readonly HotelAdminScope _scope;

    public RoomTypesController(IRoomTypeService svc, HotelAdminScope scope) : base(svc)
    {
        _scope = scope;
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PagedResult<RoomTypeResponse>> Get([FromQuery] RoomTypeSearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomTypeResponse?> GetById(int id)
    {
        return Filtered(async () => await base.GetById(id));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomTypeResponse> Create([FromBody] RoomTypeUpsertRequest req)
    {
        return Filtered(async () => await base.Create(req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomTypeResponse?> Update(int id, [FromBody] RoomTypeUpsertRequest req)
    {
        return Filtered(async () => await base.Update(id, req));
    }

    private async Task<T> Filtered<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (!hotelId.HasValue) return await action();

        if (_service is IRoomTypeService svc)
        {
            svc.ForceHotelScope(hotelId.Value);
        }

        return await action();
    }
}
