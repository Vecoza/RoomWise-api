using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Api.Auth;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Administrator)]
public sealed class RoomTypeImagesController
    : BaseCRUDController<RoomTypeImageResponse, RoomTypeImageSearchObject, RoomTypeImageUpsertRequest, RoomTypeImageUpsertRequest>
{
    private readonly IRoomTypeImageService _svc;
    private readonly HotelAdminScope _scope;

    public RoomTypeImagesController(IRoomTypeImageService svc, HotelAdminScope scope) : base(svc)
    {
        _svc = svc;
        _scope = scope;
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] RoomTypeImageReorderRequest req, CancellationToken ct)
    {
        try
        {
            var hotelId = await _scope.GetHotelIdAsync(ct);
            if (hotelId.HasValue) _svc.ForceHotelScope(hotelId.Value);
            await _svc.ReorderAsync(req, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    public override Task<PagedResult<RoomTypeImageResponse>> Get([FromQuery] RoomTypeImageSearchObject? search = null)
    {
        return Scope(() => base.Get(search));
    }

    public override Task<RoomTypeImageResponse> Create([FromBody] RoomTypeImageUpsertRequest req)
    {
        return Scope(() => base.Create(req));
    }

    public override Task<RoomTypeImageResponse?> Update(int id, [FromBody] RoomTypeImageUpsertRequest req)
    {
        return Scope(() => base.Update(id, req));
    }

    public override Task<bool> Delete(int id)
    {
        return Scope(() => base.Delete(id));
    }

    private async Task<T> Scope<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue) _svc.ForceHotelScope(hotelId.Value);
        return await action();
    }
}
