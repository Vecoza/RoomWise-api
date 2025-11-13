using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class HotelImagesController
    : BaseCRUDController<HotelImageResponse, HotelImageSearchObject, HotelImageUpsertRequest, HotelImageUpsertRequest>
{
    private readonly IHotelImageService _svc;
    public HotelImagesController(IHotelImageService svc) : base(svc) => _svc = svc;

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] HotelImageReorderRequest req, CancellationToken ct)
    {
        await _svc.ReorderAsync(req, ct);
        return NoContent();
    }
}