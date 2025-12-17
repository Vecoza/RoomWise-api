using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoomWise.Api.Data;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Background;

public sealed class ReservationReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationReminderService> _logger;

    public ReservationReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running reservation reminders.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // app is shutting down
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateTime.UtcNow.Date;
        var checkInDate = today.AddDays(1);
        var checkOutDate = today.AddDays(1);


        var upcomingCheckIns = await db.Set<Reservation>()
            .Where(r =>
                r.Status == "Confirmed" &&
                !r.CheckInReminderSent &&
                r.CheckIn.Date == checkInDate)
            .ToListAsync(ct);

        foreach (var r in upcomingCheckIns)
        {
            if (!string.IsNullOrWhiteSpace(r.UserId))
            {
                await notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = r.UserId,
                    ReservationId = r.Id,
                    Type = "checkin_reminder",
                    Message = $"Reminder: your stay at hotel {r.HotelId} starts on {r.CheckIn:yyyy-MM-dd}."
                }, ct);
            }

            r.CheckInReminderSent = true;
        }


        var upcomingCheckOuts = await db.Set<Reservation>()
            .Where(r =>
                r.Status == "Confirmed" &&
                !r.CheckOutReminderSent &&
                r.CheckOut.Date == checkOutDate)
            .ToListAsync(ct);

        foreach (var r in upcomingCheckOuts)
        {
            if (!string.IsNullOrWhiteSpace(r.UserId))
            {
                await notifications.CreateAsync(new NotificationCreateRequest()

                {
                    UserId = r.UserId,
                    ReservationId = r.Id,
                    Type = "checkout_reminder",
                    Message = $"Reminder: your stay (reservation {r.ConfirmationNumber}) ends on {r.CheckOut:yyyy-MM-dd}."
                }, ct);
            }

            r.CheckOutReminderSent = true;
        }

        if (upcomingCheckIns.Count > 0 || upcomingCheckOuts.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
