
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class RoomAvailabilitiesController
    : BaseCRUDController<RoomAvailabilityResponse, RoomAvailabilitySearchObject, RoomAvailabilityUpsertRequest, RoomAvailabilityUpsertRequest>
{
    private readonly IRoomAvailabilityService _svc;

    public RoomAvailabilitiesController(IRoomAvailabilityService svc) : base(svc)
        => _svc = svc;

    [HttpPost("batch-upsert")]
    public async Task<IActionResult> BatchUpsert([FromBody] RoomAvailabilityBatchUpsertRequest req, CancellationToken ct)
    {
        await _svc.BatchUpsertAsync(req, ct);
        return NoContent();
    }
}