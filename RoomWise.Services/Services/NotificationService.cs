using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class NotificationService : INotificationService
{
    private readonly DbContext _db;

    public NotificationService(DbContext db) => _db = db;

    public async Task<NotificationResponse> CreateAsync(
        NotificationCreateRequest request,
        CancellationToken ct = default)
    {
        var entity = new Notification
        {
            UserId        = request.UserId,
            ReservationId = request.ReservationId,
            Type          = request.Type,
            Message       = request.Message,
            IsRead        = false,
            CreatedAt     = DateTime.UtcNow
        };

        _db.Set<Notification>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToResponse(entity);
    }

    public async Task<PagedResult<NotificationResponse>> GetForUserAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page     = Math.Max(0, page);
        pageSize = Math.Max(1, pageSize);

        var q = _db.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<NotificationResponse>
        {
            Items      = items.Select(ToResponse).ToList(),
            TotalCount = total
        };
    }

    public async Task MarkAsReadAsync(
        int id,
        string userId,
        CancellationToken ct = default)
    {
        var entity = await _db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        if (entity is null) return;

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static NotificationResponse ToResponse(Notification n) => new()
    {
        Id           = n.Id,
        UserId       = n.UserId,
        ReservationId = n.ReservationId,
        Type         = n.Type,
        Message      = n.Message,
        IsRead       = n.IsRead,
        CreatedAt    = n.CreatedAt
    };
}
