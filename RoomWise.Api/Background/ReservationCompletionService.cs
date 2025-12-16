using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoomWise.Api.Data;
using RoomWise.Model;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Background;

public sealed class ReservationCompletionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationCompletionService> _logger;

    public ReservationCompletionService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationCompletionService> logger)
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
                _logger.LogError(ex, "Error while marking completed reservations.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var loyalty = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();

        var today = DateTime.UtcNow.Date;

        var toComplete = await db.Set<Reservation>()
            .Where(r =>
                (r.Status == "Pending" || r.Status == "Confirmed") &&
                r.CheckOut.Date <= today)
            .ToListAsync(ct);

        if (toComplete.Count == 0) return;

        foreach (var r in toComplete)
        {
            r.Status = "Completed";


            var hasLoyalty = await db.Set<LoyaltyPoint>()
                .AnyAsync(lp => lp.ReservationId == r.Id && lp.Delta > 0, ct);
            if (hasLoyalty) continue;

            var payment = await db.Set<Payment>()
                .Where(p => p.ReservationId == r.Id && p.Status == "Succeeded")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (payment is null) continue;

            var points = (int)Math.Floor(payment.Amount / 10m);
            if (points > 0)
            {
                await loyalty.AddAsync(
                    userId: r.UserId,
                    delta: points,
                    reason: $"Auto-complete earn for reservation {r.Id}",
                    reservationId: r.Id,
                    ct: ct);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
