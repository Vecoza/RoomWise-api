using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomWise.Api.Auth;
using RoomWise.Model;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Administrator)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly DbContext _db;
    private readonly ILoyaltyService _loyalty;
    private readonly HotelAdminScope _scope;

    public AdminUsersController(DbContext db, ILoyaltyService loyalty, HotelAdminScope scope)
    {
        _db = db;
        _loyalty = loyalty;
        _scope = scope;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserSummaryResponse>>> List(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (!hotelId.HasValue) return Forbid();

        var guestIds = await _db.Set<Reservation>()
            .Where(r => r.HotelId == hotelId.Value)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(ct);

        if (guestIds.Count == 0)
        {
            return Ok(new PagedResult<AdminUserSummaryResponse>
            {
                Items = new List<AdminUserSummaryResponse>(),
                TotalCount = 0
            });
        }

        var users = await _db.Set<AppUser>()
            .Where(u => guestIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Skip(Math.Max(0, page) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .ToListAsync(ct);

        var profiles = await _db.Set<UserProfile>()
            .Where(p => guestIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var items = new List<AdminUserSummaryResponse>();
        foreach (var u in users)
        {
            profiles.TryGetValue(u.Id, out var p);
            items.Add(new AdminUserSummaryResponse
            {
                UserId = u.Id,
                Email = u.Email ?? string.Empty,
                FirstName = p?.FirstName ?? string.Empty,
                LastName = p?.LastName ?? string.Empty,
                Phone = p?.Phone,
                LoyaltyBalance = p?.LoyaltyBalance ?? 0,
                CreatedAt = p?.CreatedAt ?? DateTime.UtcNow
            });
        }

        return Ok(new PagedResult<AdminUserSummaryResponse>
        {
            Items = items,
            TotalCount = guestIds.Count
        });
    }

    [HttpGet("{userId}/loyalty")]
    public async Task<ActionResult<object>> Loyalty(string userId, CancellationToken ct)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (!hotelId.HasValue) return Forbid();

        var hasReservation = await _db.Set<Reservation>()
            .AnyAsync(r => r.HotelId == hotelId.Value && r.UserId == userId, ct);
        if (!hasReservation) return Forbid();

        var balance = await _loyalty.GetBalanceAsync(userId, ct);
        var history = await _loyalty.GetHistoryAsync(userId, 0, 20, ct);
        return Ok(new { balance, history });
    }
}
