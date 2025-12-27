using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class HotelImagesController
    : BaseCRUDController<HotelImageResponse, HotelImageSearchObject, HotelImageUpsertRequest, HotelImageUpsertRequest>
{
    private readonly IHotelImageService _svc;
    private readonly HotelAdminScope _scope;
    public HotelImagesController(IHotelImageService svc, HotelAdminScope scope) : base(svc)
    {
        _svc = svc;
        _scope = scope;
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] HotelImageReorderRequest req, CancellationToken ct)
    {
        try
        {
            var hotelId = await _scope.GetHotelIdAsync();
            if (hotelId.HasValue)
            {
                var ids = req.Items.Select(i => i.Id).ToList();
                var allowed = await _svc.ValidateHotelAsync(hotelId.Value, ids, ct);
                if (!allowed) return Forbid();
            }

            await _svc.ReorderAsync(req, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpPost("upload")]
    public async Task<ActionResult<HotelImageResponse>> Upload(
        [FromForm] IFormFile? file,
        [FromForm] int? sortOrder,
        [FromForm] int? hotelId,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only image files are allowed." });

        var scopedHotelId = await _scope.GetHotelIdAsync(ct);
        var targetHotelId = scopedHotelId ?? hotelId;
        if (!targetHotelId.HasValue)
            return BadRequest(new { message = "HotelId is required." });

        if (scopedHotelId.HasValue)
            _svc.ForceHotelScope(scopedHotelId.Value);

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var created = await _svc.CreateAsync(new HotelImageUpsertRequest
        {
            HotelId = targetHotelId.Value,
            Url = base64,
            SortOrder = sortOrder ?? 0
        });

        return Ok(created);
    }

    public override Task<PagedResult<HotelImageResponse>> Get([FromQuery] HotelImageSearchObject? search = null)
    {
        return Scope(() => base.Get(search));
    }

    public override Task<HotelImageResponse> Create([FromBody] HotelImageUpsertRequest req)
    {
        return Scope(() => base.Create(req));
    }

    public override Task<HotelImageResponse?> Update(int id, [FromBody] HotelImageUpsertRequest req)
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
