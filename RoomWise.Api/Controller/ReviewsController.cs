
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;
using RoomWise.Model;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/reviews")]
[Authorize]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly HotelAdminScope _scope;

    public ReviewsController(IReviewService reviews, HotelAdminScope scope)
    {
        _reviews = reviews;
        _scope = scope;
    }


    [HttpPost]
    public async Task<ActionResult<ReviewResponse>> Create(
        [FromBody] ReviewUpsertRequest req,
        CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Forbid();

        req.UserId = userId;

        try
        {
            var created = await _reviews.CreateAsync(req, ct);

            return CreatedAtAction(
                nameof(HotelReviews),
                new { id = created.HotelId, page = 0, pageSize = 10 },
                created
            );
        }
        catch (InvalidOperationException ex)
        {

            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("/api/hotels/{id:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> HotelReviews(
        int id,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (hotelId.HasValue && hotelId.Value != id) return Forbid();
        var result = await _reviews.ListByHotelAsync(id, page, pageSize, ct);
        return Ok(result);
    }


}
