
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
public sealed class RoomAvailabilitiesController
    : BaseCRUDController<RoomAvailabilityResponse, RoomAvailabilitySearchObject, RoomAvailabilityUpsertRequest, RoomAvailabilityUpsertRequest>
{
    private readonly IRoomAvailabilityService _svc;
    private readonly HotelAdminScope _scope;

    public RoomAvailabilitiesController(IRoomAvailabilityService svc, HotelAdminScope scope) : base(svc)
    {
        _svc = svc;
        _scope = scope;
    }

    [HttpPost("batch-upsert")]
    public async Task<IActionResult> BatchUpsert([FromBody] RoomAvailabilityBatchUpsertRequest req, CancellationToken ct)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (hotelId.HasValue)
        {
            _svc.ForceHotelScope(hotelId.Value);
        }
        await _svc.BatchUpsertAsync(req, ct);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PagedResult<RoomAvailabilityResponse>> Get([FromQuery] RoomAvailabilitySearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomAvailabilityResponse> Create([FromBody] RoomAvailabilityUpsertRequest req)
    {
        return Filtered(async () => await base.Create(req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<RoomAvailabilityResponse?> Update(int id, [FromBody] RoomAvailabilityUpsertRequest req)
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
