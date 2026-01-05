using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
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
            .Include(r => r.Hotel).ThenInclude(h => h.City)
            .Include(r => r.RoomType)
            .Include(r => r.AddOns).ThenInclude(a => a.AddOn)
            .Where(r =>
                r.Status == "Confirmed" &&
                !r.CheckInReminderSent &&
                r.CheckIn.Date == checkInDate)
            .ToListAsync(ct);

        foreach (var r in upcomingCheckIns)
        {
            if (!string.IsNullOrWhiteSpace(r.UserId))
            {
                var firstName = await db.Set<UserProfile>()
                    .Where(p => p.UserId == r.UserId)
                    .Select(p => p.FirstName)
                    .FirstOrDefaultAsync(ct);

                var greetingName = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim();
                var emailBody = BuildReminderEmail(
                    title: "Check-in reminder",
                    intro: "Your stay starts tomorrow. Here are your reservation details:",
                    greetingName: greetingName,
                    reservation: r);

                await notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = r.UserId,
                    ReservationId = r.Id,
                    Type = "checkin_reminder",
                    Message = $"Reminder: your stay starts on {r.CheckIn:yyyy-MM-dd}.",
                    EmailBody = emailBody,
                    EmailIsHtml = true
                }, ct);
            }

            r.CheckInReminderSent = true;
        }


        var upcomingCheckOuts = await db.Set<Reservation>()
            .Include(r => r.Hotel).ThenInclude(h => h.City)
            .Include(r => r.RoomType)
            .Include(r => r.AddOns).ThenInclude(a => a.AddOn)
            .Where(r =>
                r.Status == "Confirmed" &&
                !r.CheckOutReminderSent &&
                r.CheckOut.Date == checkOutDate)
            .ToListAsync(ct);

        foreach (var r in upcomingCheckOuts)
        {
            if (!string.IsNullOrWhiteSpace(r.UserId))
            {
                var firstName = await db.Set<UserProfile>()
                    .Where(p => p.UserId == r.UserId)
                    .Select(p => p.FirstName)
                    .FirstOrDefaultAsync(ct);

                var greetingName = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim();
                var emailBody = BuildReminderEmail(
                    title: "Check-out reminder",
                    intro: "Your stay ends tomorrow. Here are your reservation details:",
                    greetingName: greetingName,
                    reservation: r);

                await notifications.CreateAsync(new NotificationCreateRequest()

                {
                    UserId = r.UserId,
                    ReservationId = r.Id,
                    Type = "checkout_reminder",
                    Message = $"Reminder: your stay ends on {r.CheckOut:yyyy-MM-dd}.",
                    EmailBody = emailBody,
                    EmailIsHtml = true
                }, ct);
            }

            r.CheckOutReminderSent = true;
        }

        if (upcomingCheckIns.Count > 0 || upcomingCheckOuts.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static string BuildReminderEmail(
        string title,
        string intro,
        string greetingName,
        Reservation reservation)
    {
        var hotelName = reservation.Hotel?.Name ?? "your hotel";
        var hotelAddress = string.Join(", ", new[]
        {
            reservation.Hotel?.AddressLine,
            reservation.Hotel?.City?.Name
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var roomType = reservation.RoomType?.Name ?? "Room";
        var currency = string.IsNullOrWhiteSpace(reservation.Currency) ? "EUR" : reservation.Currency;
        var nights = Math.Max(1, (reservation.CheckOut.Date - reservation.CheckIn.Date).Days);
        var hotelEmail = reservation.Hotel?.Email;
        var hotelWebsite = reservation.Hotel?.Website;

        var addOns = reservation.AddOns
            .Where(a => a.AddOn is not null)
            .Select(a => new
            {
                Name = a.AddOn!.Name,
                a.Quantity,
                a.LineTotal
            })
            .ToList();

        return new StringBuilder()
            .AppendLine("<!DOCTYPE html>")
            .AppendLine("<html><body style=\"font-family:Arial,Helvetica,sans-serif;color:#1f2937;\">")
            .AppendLine("<div style=\"max-width:640px;margin:0 auto;padding:24px;\">")
            .AppendLine("<h2 style=\"margin:0 0 8px;color:#0f172a;\">" + Html(title) + "</h2>")
            .AppendLine("<p style=\"margin:0 0 16px;\">Hi " + Html(greetingName) + ",</p>")
            .AppendLine("<p style=\"margin:0 0 16px;\">" + Html(intro) + "</p>")
            .AppendLine("<div style=\"padding:12px 16px;border:1px solid #e2e8f0;background:#f8fafc;border-radius:8px;margin-bottom:16px;\">")
            .AppendLine("<strong>Confirmation:</strong> " + Html(reservation.ConfirmationNumber) + "<br/>")
            .AppendLine("<strong>Reservation ID:</strong> " + Html(reservation.PublicId.ToString()) + "<br/>")
            .AppendLine("<strong>Status:</strong> " + Html(reservation.Status) + "<br/>")
            .AppendLine("<strong>Nights:</strong> " + nights + "<br/>")
            .AppendLine("<strong>Total:</strong> " + reservation.Total.ToString("0.00") + " " + Html(currency) + "</div>")
            .AppendLine("<h3 style=\"margin:16px 0 8px;\">Hotel</h3>")
            .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
            .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Name</td><td style=\"padding:4px 0;\">" + Html(hotelName) + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Address</td><td style=\"padding:4px 0;\">" + Html(hotelAddress) + "</td></tr>")
            .AppendLine(string.IsNullOrWhiteSpace(hotelEmail)
                ? ""
                : "<tr><td style=\"padding:4px 0;\">Email</td><td style=\"padding:4px 0;\">" + Html(hotelEmail) + "</td></tr>")
            .AppendLine(string.IsNullOrWhiteSpace(hotelWebsite)
                ? ""
                : "<tr><td style=\"padding:4px 0;\">Website</td><td style=\"padding:4px 0;\">" + Html(hotelWebsite) + "</td></tr>")
            .AppendLine("</table>")
            .AppendLine("<h3 style=\"margin:16px 0 8px;\">Stay</h3>")
            .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
            .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Room type</td><td style=\"padding:4px 0;\">" + Html(roomType) + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Check-in</td><td style=\"padding:4px 0;\">" + reservation.CheckIn.ToString("yyyy-MM-dd") + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Check-out</td><td style=\"padding:4px 0;\">" + reservation.CheckOut.ToString("yyyy-MM-dd") + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Guests</td><td style=\"padding:4px 0;\">" + reservation.Guests + "</td></tr>")
            .AppendLine("</table>")
            .AppendLine("<h3 style=\"margin:16px 0 8px;\">Add-ons</h3>")
            .AppendLine(addOns.Count == 0
                ? "<p style=\"margin:0 0 16px;\">None</p>"
                : "<table style=\"width:100%;border-collapse:collapse;\">" +
                  string.Join("", addOns.Select(a =>
                      "<tr><td style=\"padding:4px 0;width:240px;\">" + Html(a.Name) + "</td>" +
                      "<td style=\"padding:4px 0;\">x" + a.Quantity + "</td>" +
                      "<td style=\"padding:4px 0;text-align:right;\">" + a.LineTotal.ToString("0.00") + " " + Html(currency) + "</td></tr>")) +
                  "</table>")
            .AppendLine("<h3 style=\"margin:16px 0 8px;\">Charges</h3>")
            .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
            .AppendLine("<tr><td style=\"padding:4px 0;width:140px;\">Subtotal</td><td style=\"padding:4px 0;text-align:right;\">" + reservation.Subtotal.ToString("0.00") + " " + Html(currency) + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Taxes & fees</td><td style=\"padding:4px 0;text-align:right;\">" + reservation.TaxesAndFees.ToString("0.00") + " " + Html(currency) + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;\">Service fee</td><td style=\"padding:4px 0;text-align:right;\">" + reservation.ServiceFee.ToString("0.00") + " " + Html(currency) + "</td></tr>")
            .AppendLine("<tr><td style=\"padding:4px 0;font-weight:bold;\">Total</td><td style=\"padding:4px 0;text-align:right;font-weight:bold;\">" + reservation.Total.ToString("0.00") + " " + Html(currency) + "</td></tr>")
            .AppendLine("</table>")
            .AppendLine("<p style=\"margin:16px 0 0;\">Please review your booking details in the app if you need to make changes.</p>")
            .AppendLine("<p style=\"margin:16px 0 0;\">Need help? Reply to this email and we will assist you.</p>")
            .AppendLine("<p style=\"margin:24px 0 0;\">RoomWise Team</p>")
            .AppendLine("</div></body></html>")
            .ToString();
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
