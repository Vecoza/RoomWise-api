using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search) => _search = search;

    [HttpGet("hotels")]
    public async Task<ActionResult<PagedResult<HotelSearchItemResponse>>> Hotels(
        [FromQuery] HotelSearchRequest req, CancellationToken ct)
    {
        if (req.CheckIn == default || req.CheckOut == default)
            return BadRequest("checkIn and checkOut are required.");
        if (req.Guests < 1)
            return BadRequest("guests must be >= 1.");

        if (req.Page <= 0) req.Page = 0;
        if (req.PageSize <= 0 || req.PageSize > 100) req.PageSize = 20;

        var result = await _search.SearchHotelsAsync(req, ct);
        return Ok(result);
    }
}