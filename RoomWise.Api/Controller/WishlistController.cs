using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{AppRoles.Guest},{AppRoles.Administrator}")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;

    public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    [HttpPost("{hotelId:int}")]
    public async Task<IActionResult> Add(int hotelId)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var ok = await _wishlist.AddAsync(userId!, hotelId);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{hotelId:int}")]
    public async Task<IActionResult> Remove(int hotelId)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        await _wishlist.RemoveAsync(userId!, hotelId);
        return NoContent();
    }

    [HttpGet("")]
    public async Task<ActionResult<IReadOnlyList<HotelSearchItemResponse>>> List()
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var items = await _wishlist.ListAsync(userId!);
        return Ok(items);
    }

    private string? GetUserIdOrForbid(out ActionResult? forbid)
    {
        forbid = null;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            forbid = Forbid();
            return null;
        }
        return userId;
    }

}