// RoomWise.Api/Controller/ReviewsController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class ReviewsController
    : BaseCRUDController<ReviewResponse, ReviewSearchObject, ReviewUpsertRequest, ReviewUpsertRequest>
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews) : base(reviews) => _reviews = reviews;

    [HttpPost]
    public async Task<ActionResult<ReviewResponse>> Create([FromBody] ReviewUpsertRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        req.UserId = userId;

        var created = await _reviews.CreateAsync(req, ct);
        return CreatedAtAction(nameof(HotelReviews), new { id = created.HotelId }, created);
    }

    [HttpGet("/api/hotels/{id:int}/reviews")]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> HotelReviews(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _reviews.ListByHotelAsync(id, page, pageSize, ct);
        return Ok(result);
    }
}