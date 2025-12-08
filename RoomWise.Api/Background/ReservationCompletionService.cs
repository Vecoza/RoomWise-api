using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoomWise.Api.Data;
using RoomWise.Model;

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
        }

        await db.SaveChangesAsync(ct);
    }
}
