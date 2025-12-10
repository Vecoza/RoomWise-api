using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/me/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
        => _notifications = notifications;

    private string? GetUserIdOrForbid(out ActionResult? forbid)
    {
        forbid = null;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            forbid = Forbid();
            return null;
        }

        return userId;
    }

    // GET /api/me/notifications?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> List(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var result = await _notifications.GetForUserAsync(userId!, page, pageSize, ct);
        return Ok(result);
    }

    // POST /api/me/notifications
    // Creates a notification for the current user.
    [HttpPost]
    public async Task<ActionResult<NotificationResponse>> Create(
        [FromBody] NotificationCreateRequest req,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        // override any client-sent UserId
        req.UserId = userId!;
        var created = await _notifications.CreateAsync(req, ct);
        return Ok(created);
    }

    // POST /api/me/notifications/{id}/read
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        await _notifications.MarkAsReadAsync(id, userId!, ct);
        return NoContent();
    }
}
