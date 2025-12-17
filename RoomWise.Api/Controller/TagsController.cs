
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]

public class TagsController
    : BaseCRUDController<TagResponse, TagSearchObject, TagUpsertRequest, TagUpsertRequest>
{
    private readonly ITagService _svc;
    public TagsController(ITagService svc) : base(svc) => _svc = svc;

    [HttpPut("hotel/{hotelId:int}")]
    public async Task<IActionResult> SetForHotel(int hotelId, [FromBody] IEnumerable<int> tagIds, CancellationToken ct)
    {
        await _svc.SetForHotelAsync(hotelId, tagIds, ct);
        return NoContent();
    }
}