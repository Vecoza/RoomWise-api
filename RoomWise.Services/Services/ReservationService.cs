

using System.Security.Claims;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Microsoft.AspNetCore.Http;
using Stripe;

namespace RoomWise.Services.Services;

public sealed class ReservationService
    : BaseCRUDService<ReservationResponse, ReservationSearchObject, Reservation, ReservationUpsertRequest, ReservationUpsertRequest>,
        IReservationService
{
    private readonly IRoomAvailabilityService _availability;
    private readonly INotificationService _notifications;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILoyaltyService _loyalty;
    private int? _forcedHotelId;

    public ReservationService(
        DbContext context,
        IMapper mapper,
        IRoomAvailabilityService availability,
        INotificationService notifications,
        IHttpContextAccessor httpContextAccessor,
        ILoyaltyService loyalty)
        : base(context, mapper)
    {
        _availability = availability;
        _notifications = notifications;
        _httpContextAccessor = httpContextAccessor;
        _loyalty = loyalty;
    }

    public void ForceHotelScope(int hotelId) => _forcedHotelId = hotelId;

    protected override IQueryable<Reservation> ApplyFilter(IQueryable<Reservation> q, ReservationSearchObject s)
    {
        if (_forcedHotelId.HasValue)
        {
            s.HotelId = _forcedHotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(s.UserId))
            q = q.Where(x => x.UserId == s.UserId);

        if (s.HotelId.HasValue)
            q = q.Where(x => x.HotelId == s.HotelId.Value);

        if (s.RoomTypeId.HasValue)
            q = q.Where(x => x.RoomTypeId == s.RoomTypeId.Value);

        if (!string.IsNullOrWhiteSpace(s.Status))
            q = q.Where(x => x.Status == s.Status);

        if (s.FromCheckIn.HasValue)
            q = q.Where(x => x.CheckIn >= s.FromCheckIn.Value);

        if (s.ToCheckIn.HasValue)
            q = q.Where(x => x.CheckIn <= s.ToCheckIn.Value);

        return q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
    }

    public override async Task<ReservationResponse> CreateAsync(ReservationUpsertRequest request)
        => MapToResponse(await CreateReservationCoreAsync(request, sendNotification: true, CancellationToken.None));

    public async Task<ReservationResponse> CreateWithPaymentIntentAsync(ReservationUpsertRequest request)
        => MapToResponse(await CreateReservationCoreAsync(request, sendNotification: false, CancellationToken.None));

    private async Task<Reservation> CreateReservationCoreAsync(ReservationUpsertRequest request, bool sendNotification, CancellationToken ct)
    {

        var checkIn = request.CheckIn.Date;
        var checkOut = request.CheckOut.Date;

        if (checkOut <= checkIn) throw new ArgumentException("CheckOut must be after CheckIn.");
        if (request.Guests <= 0) throw new ArgumentException("Guests must be >= 1.");

        var roomType = await _context.Set<RoomType>().FindAsync(request.RoomTypeId);
        if (roomType is null) throw new InvalidOperationException("RoomType not found.");
        if (request.Guests > roomType.Capacity)
            throw new InvalidOperationException("Guests exceed room type capacity.");

        if (request.HotelId != roomType.HotelId)
            throw new InvalidOperationException("Room type does not belong to the specified hotel.");

        var nights = (checkOut - checkIn).Days;
        if (nights <= 0) throw new ArgumentException("Invalid date range.");

        var dbRate = await _context.Set<RoomRate>()
            .Where(r => r.RoomTypeId == request.RoomTypeId
                        && r.StartDate <= checkIn
                        && r.EndDate >= checkOut)
            .Select(r => (decimal?)r.Price)
            .MinAsync(ct);

        var nightly = dbRate ?? roomType.BasePrice;

        var roomTotal = nightly * nights;

        decimal addOnsTotal = 0m;
        var addOnItems = request.AddOns ?? new List<ReservationAddOnItem>();
        Dictionary<int, AddOn> addOnById = new();

        if (addOnItems.Count > 0)
        {
            var addOnIds = addOnItems.Select(a => a.AddOnId).Distinct().ToList();

            var addOns = await _context.Set<AddOn>()
                .Where(a => addOnIds.Contains(a.Id))
                .ToListAsync(ct);

            addOnById = addOns.ToDictionary(a => a.Id);

            foreach (var item in addOnItems)
            {
                if (!addOnById.TryGetValue(item.AddOnId, out var addOn))
                    throw new InvalidOperationException($"Add-on {item.AddOnId} not found.");

                if (addOn.HotelId != roomType.HotelId)
                    throw new InvalidOperationException("Add-on does not belong to this hotel.");

                if (item.Quantity <= 0)
                    throw new ArgumentException("Add-on quantity must be >= 1.");

                addOnsTotal += CalculateAddOnLineTotal(addOn, item, nights, request.Guests);
            }
        }

        var entity = new Reservation();
        MapInsertToEntity(entity, request);

        var userId = _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Authenticated user id not found.");

        entity.UserId = userId;
        entity.HotelId = roomType.HotelId;
        entity.RoomTypeId = roomType.Id;

        var promo = await ResolvePromotionAsync(request, roomType.HotelId, checkIn, checkOut, nights, ct);
        if (promo is not null)
        {
            entity.PromotionId = promo.Id;
        }

        var subtotalBeforePromo = roomTotal + addOnsTotal;
        var discountedSubtotal = ApplyPromotion(subtotalBeforePromo, promo);

        entity.PublicId = entity.PublicId == Guid.Empty ? Guid.NewGuid() : entity.PublicId;
        entity.CheckIn = checkIn;
        entity.CheckOut = checkOut;
        entity.Subtotal = discountedSubtotal;
        entity.Total = entity.Subtotal + entity.TaxesAndFees + entity.ServiceFee;
        entity.Currency = roomType.Currency;
        entity.CreatedAt = DateTime.UtcNow;
        entity.Status = "Pending";
        entity.ConfirmationNumber = GenerateConfirmationNumber();



        using var tx = await _context.Database.BeginTransactionAsync(ct);

        var hasRoomAvailability = _context.Model.FindEntityType(typeof(RoomAvailability)) is not null;
        if (hasRoomAvailability)
        {

            await _availability.EnsureRangeConfiguredAsync(entity.RoomTypeId, checkIn, checkOut);
            var ok = await _availability.TryConsumeRangeAsync(entity.RoomTypeId, checkIn, checkOut);
            if (!ok) throw new InvalidOperationException("No availability for selected dates.");
        }
        else
        {

            var stock = roomType.Stock;

            for (int i = 0; i < nights; i++)
            {
                var dayStart = checkIn.AddDays(i);
                var dayEnd = dayStart.AddDays(1);

                var overlappingCount = await _context.Set<Reservation>()
                    .Where(r => r.RoomTypeId == request.RoomTypeId
                             && r.Status != "Cancelled"
                             && r.CheckIn < dayEnd
                             && r.CheckOut > dayStart)
                    .CountAsync(ct);

                if (overlappingCount >= stock)
                    throw new InvalidOperationException($"No availability on {dayStart:yyyy-MM-dd}.");
            }
        }

        _context.Set<Reservation>().Add(entity);

        var balance = await _loyalty.GetBalanceAsync(userId, ct);
        var redeem = (int)Math.Min(balance, Math.Floor(entity.Total));
        if (redeem > 0)
        {
            entity.Total -= redeem;
            if (entity.Total < 0) entity.Total = 0;
        }

        await _context.SaveChangesAsync(ct);


        if (addOnItems.Count > 0 && addOnById.Count > 0)
        {
            var reservationAddOns = new List<ReservationAddOn>();

            foreach (var item in addOnItems)
            {
                if (!addOnById.TryGetValue(item.AddOnId, out var addOn))
                    continue;

                var lineTotal = CalculateAddOnLineTotal(addOn, item, nights, request.Guests);

                reservationAddOns.Add(new ReservationAddOn
                {
                    ReservationId = entity.Id,
                    AddOnId = addOn.Id,
                    Quantity = item.Quantity,
                    UnitPrice = addOn.Price,
                    LineTotal = lineTotal
                });
            }

            if (reservationAddOns.Count > 0)
            {
                _context.Set<ReservationAddOn>().AddRange(reservationAddOns);
                await _context.SaveChangesAsync(ct);
            }
        }

        await tx.CommitAsync(ct);

        if (redeem > 0)
        {
            await _loyalty.AddAsync(
                userId: entity.UserId,
                delta: -redeem,
                reason: $"Redeem {redeem} points for reservation {entity.Id}",
                reservationId: entity.Id,
                ct: ct);
        }

        if (sendNotification)
        {
            await SendReservationCreatedNotificationAsync(entity);
        }

        return entity;
    }

    protected override Task BeforeUpdate(Reservation entity, ReservationUpsertRequest request)
    {
        entity.Total = entity.Subtotal + entity.TaxesAndFees + entity.ServiceFee;
        return Task.CompletedTask;
    }


    public async Task CancelAsync(int id, CancellationToken ct)
    {
        await CancelInternalAsync(id, allowAdminOverride: false, ct);
    }

    public async Task CancelAsAdminAsync(int id, CancellationToken ct)
    {
        await CancelInternalAsync(id, allowAdminOverride: true, ct);
    }

    private async Task CancelInternalAsync(int id, bool allowAdminOverride, CancellationToken ct)
    {
        var userId = _httpContextAccessor.HttpContext?
        .User
        .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Authenticated user id not found.");

        var reservation = await _context.Set<Reservation>()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reservation is null)
            throw new KeyNotFoundException($"Reservation {id} not found.");

        if (!allowAdminOverride && !string.Equals(reservation.UserId, userId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("You do not own this reservation.");

        if (reservation.Status == "Cancelled")
            return;

        if (reservation.Status == "Completed")
            throw new InvalidOperationException("Completed reservation cannot be cancelled.");

        if (reservation.Status != "Pending" && reservation.Status != "Confirmed")
            throw new InvalidOperationException("Reservation cannot be cancelled in its current status.");

        if (DateTime.UtcNow >= reservation.CheckIn.AddDays(-1))
            throw new InvalidOperationException("Reservation can only be cancelled at least 24 hours before check-in.");

        using var tx = await _context.Database.BeginTransactionAsync(ct);

        reservation.Status = "Cancelled";
        reservation.CancelledAt = DateTime.UtcNow;

        var hasRoomAvailability = _context.Model.FindEntityType(typeof(RoomAvailability)) is not null;
        if (hasRoomAvailability)
        {
            await _availability.RestoreRangeAsync(
                reservation.RoomTypeId,
                reservation.CheckIn.Date,
                reservation.CheckOut.Date);
        }

        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        try
        {
            if (!string.IsNullOrWhiteSpace(reservation.UserId))
            {
                var reservationDetails = await _context.Set<Reservation>()
                    .Include(r => r.Hotel).ThenInclude(h => h.City)
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.Id == reservation.Id, ct);

                var details = reservationDetails ?? reservation;

                var firstName = await _context.Set<UserProfile>()
                    .Where(p => p.UserId == details.UserId)
                    .Select(p => p.FirstName)
                    .FirstOrDefaultAsync(ct);

                var greetingName = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim();
                var message = $"Your reservation {details.ConfirmationNumber} has been cancelled.";

                var emailBody = new StringBuilder()
                    .AppendLine("<!DOCTYPE html>")
                    .AppendLine("<html><body style=\"font-family:Arial,Helvetica,sans-serif;color:#1f2937;\">")
                    .AppendLine("<div style=\"max-width:640px;margin:0 auto;padding:24px;\">")
                    .AppendLine("<h2 style=\"margin:0 0 8px;color:#b91c1c;\">Reservation Cancelled</h2>")
                    .AppendLine("<p style=\"margin:0 0 16px;\">Hi " + Html(greetingName) + ",</p>")
                    .AppendLine("<p style=\"margin:0 0 16px;\">Your reservation has been cancelled. Here are the details:</p>")
                    .AppendLine("<div style=\"padding:12px 16px;border:1px solid #fee2e2;background:#fef2f2;border-radius:8px;margin-bottom:16px;\">")
                    .AppendLine("<strong>Confirmation:</strong> " + Html(details.ConfirmationNumber) + "<br/>")
                    .AppendLine("<strong>Status:</strong> Cancelled<br/>")
                    .AppendLine("<strong>Cancelled at:</strong> " + reservation.CancelledAt?.ToString("yyyy-MM-dd HH:mm") + " UTC</div>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Hotel</h3>")
                    .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
                    .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Name</td><td style=\"padding:4px 0;\">" + Html(details.Hotel?.Name) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Address</td><td style=\"padding:4px 0;\">" + Html(details.Hotel?.AddressLine) + ", " + Html(details.Hotel?.City?.Name) + "</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Stay</h3>")
                    .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
                    .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Room type</td><td style=\"padding:4px 0;\">" + Html(details.RoomType?.Name) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Check-in</td><td style=\"padding:4px 0;\">" + details.CheckIn.ToString("yyyy-MM-dd") + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Check-out</td><td style=\"padding:4px 0;\">" + details.CheckOut.ToString("yyyy-MM-dd") + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Guests</td><td style=\"padding:4px 0;\">" + details.Guests + "</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<p style=\"margin:16px 0 0;\">If this was a mistake, feel free to book again anytime.</p>")
                    .AppendLine("<p style=\"margin:16px 0 0;\">Need help? Reply to this email and we will assist you.</p>")
                    .AppendLine("<p style=\"margin:24px 0 0;\">RoomWise Team</p>")
                    .AppendLine("</div></body></html>")
                    .ToString();

                await _notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = reservation.UserId,
                    ReservationId = reservation.Id,
                    Type = "reservation_cancelled",
                    Message = message,
                    EmailBody = emailBody,
                    EmailIsHtml = true
                });
            }
        }
        catch
        {
        }
    }



    public async Task<PagedResult<ReservationResponse>> GetMyAsync(string userId, string? category)
    {
        var q = _context.Set<Reservation>()
            .Include(r => r.Hotel)
            .ThenInclude(h => h.City)
            .Include(r => r.Hotel)
            .ThenInclude(h => h.Images)
            .Include(r => r.Payments)
            .Include(r => r.AddOns)
            .ThenInclude(ra => ra.AddOn)
            .Where(r => r.UserId == userId);

        var today = DateTime.UtcNow.Date;
        if (!string.IsNullOrWhiteSpace(category))
        {
            switch (category.Trim().ToLowerInvariant())
            {
                case "current":
                    q = q.Where(r =>
                        (r.Status == "Pending" || r.Status == "Confirmed") &&
                        r.CheckOut.Date >= today);
                    break;

                case "past":
                    q = q.Where(r =>
                        r.Status == "Completed" ||
                        ((r.Status == "Pending" || r.Status == "Confirmed") && r.CheckOut.Date < today));
                    break;

                case "cancelled":
                    q = q.Where(r => r.Status == "Cancelled");
                    break;
            }
        }

        q = q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

        var total = await q.CountAsync();
        var items = await q.ToListAsync();

        return new PagedResult<ReservationResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<ReservationArrivalResponse>> GetArrivalsAsync(DateTime date, CancellationToken ct)
    {
        var target = date.Date;

        var reservations = _context.Set<Reservation>()
            .AsNoTracking()
            .Where(r => r.CheckIn == target);

        if (_forcedHotelId.HasValue)
            reservations = reservations.Where(r => r.HotelId == _forcedHotelId.Value);

        var activeStatuses = new[] { "Pending", "Confirmed" };
        reservations = reservations.Where(r => activeStatuses.Contains(r.Status));

        var query =
            from r in reservations
            join rt in _context.Set<RoomType>().AsNoTracking() on r.RoomTypeId equals rt.Id
            join p in _context.Set<UserProfile>().AsNoTracking() on r.UserId equals p.UserId into profiles
            from p in profiles.DefaultIfEmpty()
            select new ReservationArrivalResponse
            {
                ReservationId = r.Id,
                GuestFirstName = p != null ? p.FirstName : string.Empty,
                GuestLastName = p != null ? p.LastName : string.Empty,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = rt.Name,
                Guests = r.Guests,
                RoomTotal = r.Total,
                Currency = r.Currency,
                CheckIn = r.CheckIn
            };

        return await query
            .OrderBy(x => x.RoomTypeName)
            .ThenBy(x => x.GuestLastName)
            .ToListAsync(ct);
    }








    private static string GenerateConfirmationNumber()
    {
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"RW-{DateTime.UtcNow:yyyyMMddHHmmss}-{token}";
    }

    private static decimal ApplyPromotion(decimal subtotal, Promotion? promo)
    {
        if (promo is null) return subtotal;
        var result = subtotal;
        if (promo.DiscountPercent.HasValue)
            result = result * (1 - promo.DiscountPercent.Value / 100m);
        if (promo.DiscountFixed.HasValue)
            result = result - promo.DiscountFixed.Value;
        return result < 0 ? 0 : result;
    }

    private async Task<Promotion?> ResolvePromotionAsync(
        ReservationUpsertRequest request,
        int hotelId,
        DateTime checkIn,
        DateTime checkOut,
        int nights,
        CancellationToken ct)
    {
        Promotion? promo = null;

        if (request.PromotionId.HasValue)
        {
            promo = await _context.Set<Promotion>()
                .FirstOrDefaultAsync(p =>
                    p.Id == request.PromotionId.Value &&
                    p.IsActive &&
                    p.HotelId == hotelId &&
                    p.StartDate <= checkIn.Date &&
                    p.EndDate >= checkOut.Date &&
                    p.MinNights <= nights, ct);
        }
        else
        {
            promo = await _context.Set<Promotion>()
                .Where(p => p.IsActive
                            && p.HotelId == hotelId
                            && p.StartDate <= checkIn.Date
                            && p.EndDate >= checkOut.Date
                            && p.MinNights <= nights)
                .OrderBy(p => p.EndDate)
                .FirstOrDefaultAsync(ct);
        }

        return promo;
    }

    private static decimal CalculateAddOnLineTotal(AddOn addOn, ReservationAddOnItem item, int nights, int guests)
    {
        var model = addOn.PricingModel ?? "";
        var perNight = string.Equals(model, "PerNight", StringComparison.OrdinalIgnoreCase);
        var perDay = model.Equals("PerDay", StringComparison.OrdinalIgnoreCase);
        var perGuestPerNight = string.Equals(model, "PerGuestPerNight", StringComparison.OrdinalIgnoreCase);

        if (perNight) return addOn.Price * item.Quantity * nights;
        if (perDay)
        {
            var days = nights + 1;
            return addOn.Price * item.Quantity * days;
        }
        if (perGuestPerNight) return addOn.Price * item.Quantity * nights * guests;

        return addOn.Price * item.Quantity;
    }

    private async Task SendReservationCreatedNotificationAsync(Reservation entity)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(entity.UserId))
            {
                var reservation = await _context.Set<Reservation>()
                    .Include(r => r.Hotel).ThenInclude(h => h.City)
                    .Include(r => r.RoomType)
                    .Include(r => r.AddOns).ThenInclude(ra => ra.AddOn)
                    .FirstOrDefaultAsync(r => r.Id == entity.Id);

                if (reservation is null) return;

                var firstName = await _context.Set<UserProfile>()
                    .Where(p => p.UserId == reservation.UserId)
                    .Select(p => p.FirstName)
                    .FirstOrDefaultAsync();

                var greetingName = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName.Trim();
                var addOns = reservation.AddOns?.ToList() ?? new List<ReservationAddOn>();
                var addOnLines = addOns.Count == 0
                    ? "<li>None</li>"
                    : string.Join("", addOns.Select(a =>
                        $"<li>{Html(a.AddOn?.Name ?? "Add-on")} x{a.Quantity}: {a.LineTotal:0.00} {Html(reservation.Currency)}</li>"));

                var message = $"Your reservation {reservation.ConfirmationNumber} has been created.";

                var emailBody = new StringBuilder()
                    .AppendLine("<!DOCTYPE html>")
                    .AppendLine("<html><body style=\"font-family:Arial,Helvetica,sans-serif;color:#1f2937;\">")
                    .AppendLine("<div style=\"max-width:640px;margin:0 auto;padding:24px;\">")
                    .AppendLine("<h2 style=\"margin:0 0 8px;\">RoomWise Booking Confirmation</h2>")
                    .AppendLine("<p style=\"margin:0 0 16px;\">Hi " + Html(greetingName) + ",</p>")
                    .AppendLine("<p style=\"margin:0 0 16px;\">Thanks for your reservation! Here is your booking summary:</p>")
                    .AppendLine("<div style=\"padding:12px 16px;border:1px solid #e5e7eb;border-radius:8px;margin-bottom:16px;\">")
                    .AppendLine("<strong>Confirmation:</strong> " + Html(reservation.ConfirmationNumber) + "<br/>")
                    .AppendLine("<strong>Status:</strong> " + Html(reservation.Status) + "</div>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Hotel</h3>")
                    .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
                    .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Name</td><td style=\"padding:4px 0;\">" + Html(reservation.Hotel?.Name) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Address</td><td style=\"padding:4px 0;\">" + Html(reservation.Hotel?.AddressLine) + ", " + Html(reservation.Hotel?.City?.Name) + "</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Stay</h3>")
                    .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
                    .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Room type</td><td style=\"padding:4px 0;\">" + Html(reservation.RoomType?.Name) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Check-in</td><td style=\"padding:4px 0;\">" + reservation.CheckIn.ToString("yyyy-MM-dd") + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Check-out</td><td style=\"padding:4px 0;\">" + reservation.CheckOut.ToString("yyyy-MM-dd") + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Guests</td><td style=\"padding:4px 0;\">" + reservation.Guests + "</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Add-ons</h3>")
                    .AppendLine("<ul style=\"margin:0 0 16px;padding-left:18px;\">" + addOnLines + "</ul>")
                    .AppendLine("<h3 style=\"margin:16px 0 8px;\">Charges</h3>")
                    .AppendLine("<table style=\"width:100%;border-collapse:collapse;\">")
                    .AppendLine("<tr><td style=\"padding:4px 0;width:120px;\">Subtotal</td><td style=\"padding:4px 0;\">" + reservation.Subtotal.ToString("0.00") + " " + Html(reservation.Currency) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Taxes &amp; fees</td><td style=\"padding:4px 0;\">" + reservation.TaxesAndFees.ToString("0.00") + " " + Html(reservation.Currency) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:4px 0;\">Service fee</td><td style=\"padding:4px 0;\">" + reservation.ServiceFee.ToString("0.00") + " " + Html(reservation.Currency) + "</td></tr>")
                    .AppendLine("<tr><td style=\"padding:6px 0;font-weight:bold;\">Total</td><td style=\"padding:6px 0;font-weight:bold;\">" + reservation.Total.ToString("0.00") + " " + Html(reservation.Currency) + "</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<p style=\"margin:16px 0 0;color:#374151;\">Loyalty points are added only after your stay is completed.</p>")
                    .AppendLine("<p style=\"margin:16px 0 0;\">Need help? Reply to this email and we will assist you.</p>")
                    .AppendLine("<p style=\"margin:24px 0 0;\">RoomWise Team</p>")
                    .AppendLine("</div></body></html>")
                    .ToString();

                await _notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = entity.UserId,
                    ReservationId = entity.Id,
                    Type = "reservation_created",
                    Message = message,
                    EmailBody = emailBody,
                    EmailIsHtml = true
                });
            }
        }
        catch
        {
        }
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public override async Task<ReservationResponse?> GetByIdAsync(int id) => await base.GetByIdAsync(id);

    public async Task<ReservationResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    {
        var entity = await _context.Set<Reservation>()
            .Include(r => r.Hotel).ThenInclude(h => h.City)
            .Include(r => r.Hotel).ThenInclude(h => h.Images)
            .Include(r => r.AddOns).ThenInclude(ra => ra.AddOn)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.PublicId == publicId, ct);

        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<(PaymentResponse Payment, string ClientSecret)?> FindActivePaymentAsync(int reservationId)
    {
        var activeStatuses = new[] { "RequiresPaymentMethod", "RequiresAction", "Processing" };

        var payment = await _context.Set<Payment>()
            .Where(p => p.ReservationId == reservationId && activeStatuses.Contains(p.Status))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (payment is null) return null;

        var resp = _mapper.Map<PaymentResponse>(payment);
        return (resp, string.Empty);
    }

    protected override ReservationResponse MapToResponse(Reservation entity)
    {
        var resp = base.MapToResponse(entity);

        resp.HotelName = entity.Hotel?.Name ?? resp.HotelName;
        resp.City = entity.Hotel?.City?.Name ?? resp.City;
        resp.ThumbnailUrl = entity.Hotel?.Images?
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault() ?? resp.ThumbnailUrl;

        var latestPayment = entity.Payments?
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (latestPayment != null)
        {
            resp.AmountPaid = latestPayment.Amount;
            resp.Total = latestPayment.Amount;
        }
        else
        {
            resp.AmountPaid = entity.Total;
        }

        var userId = _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        var reviews = _context.Set<RoomWise.Model.Review>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            resp.HasReview = reviews.Any(rv => rv.ReservationId == entity.Id && rv.UserId == userId);
        }
        else
        {
            resp.HasReview = reviews.Any(rv => rv.ReservationId == entity.Id);
        }

        return resp;
    }



}
