using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Messaging;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Services.Services;

public class NotificationService : INotificationService
{
    private readonly DbContext _db;
    private readonly IEmailQueueService _emailQueue;

    public NotificationService(DbContext db, IEmailQueueService emailQueue)
    {
        _db = db;
        _emailQueue = emailQueue;
    }

    public async Task<NotificationResponse> CreateAsync(
        NotificationCreateRequest request,
        CancellationToken ct = default)
    {
        var entity = new Notification
        {
            UserId = request.UserId,
            ReservationId = request.ReservationId,
            Type = request.Type,
            Message = request.Message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<Notification>().Add(entity);
        await _db.SaveChangesAsync(ct);

        await TryEnqueueEmailAsync(entity, request.EmailBody, request.EmailIsHtml, ct);

        return ToResponse(entity);
    }

    public async Task<PagedResult<NotificationResponse>> GetForUserAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(0, page);
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
            Items = items.Select(ToResponse).ToList(),
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

    private async Task TryEnqueueEmailAsync(
        Notification notification,
        string? emailBody,
        bool? emailIsHtml,
        CancellationToken ct)
    {
        try
        {
            var email = await _db.Set<AppUser>()
                .Where(u => u.Id == notification.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(email)) return;

            var subject = notification.Type switch
            {
                "reservation_created" => "Your reservation was created",
                "payment_succeeded" => "Payment confirmed",
                "reservation_reminder" => "Reservation reminder",
                "checkin_reminder" => "Check-in reminder",
                "checkout_reminder" => "Check-out reminder",
                "reservation_cancelled" => "Reservation cancelled",
                _ => "RoomWise notification"
            };

            await _emailQueue.PublishAsync(new EmailMessage
            {
                To = email,
                Subject = subject,
                Body = emailBody ?? notification.Message,
                IsHtml = emailIsHtml ?? false,
                ReservationId = notification.ReservationId,
                UserId = notification.UserId
            }, ct);
        }
        catch
        {

        }
    }

    private static NotificationResponse ToResponse(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        ReservationId = n.ReservationId,
        Type = n.Type,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
