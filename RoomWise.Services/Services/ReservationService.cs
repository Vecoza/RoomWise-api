

using System.Security.Claims;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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


    protected override IQueryable<Reservation> ApplyFilter(IQueryable<Reservation> q, ReservationSearchObject s)
    {
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

    //InserAsync
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

        // 1) Try to get room rate from DB (nullable)
        var dbRate = await _context.Set<RoomRate>()
            .Where(r => r.RoomTypeId == request.RoomTypeId
                        && r.StartDate <= checkIn
                        && r.EndDate >= checkOut)
            .Select(r => (decimal?)r.Price)
            .MinAsync(ct);

        // 2) Fallback to BasePrice in memory
        var nightly = dbRate ?? roomType.BasePrice;

        var roomTotal = nightly * nights;

        // 2) Add-ons
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

        // 3) Create entity
        var entity = new Reservation();
        MapInsertToEntity(entity, request);   // 1) map from request

        var userId = _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Authenticated user id not found.");

        entity.UserId = userId;
        entity.HotelId = roomType.HotelId;
        entity.RoomTypeId = roomType.Id;

        // Apply promotion if supplied or applicable
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

        // 4) Auto-redeem all loyalty points up to the total
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
                    continue; // already validated above but safe-guard

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

        // Deduct loyalty after reservation is persisted (once)
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
        var userId = _httpContextAccessor.HttpContext?
        .User
        .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Authenticated user id not found.");

        var reservation = await _context.Set<Reservation>()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reservation is null)
            throw new KeyNotFoundException($"Reservation {id} not found.");

        // Ownership check
        if (!string.Equals(reservation.UserId, userId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("You do not own this reservation.");

        if (reservation.Status == "Cancelled")
            return;

        if (reservation.Status == "Completed")
            throw new InvalidOperationException("Completed reservation cannot be cancelled.");

        if (reservation.Status != "Pending" && reservation.Status != "Confirmed")
            throw new InvalidOperationException("Reservation cannot be cancelled in its current status.");

        // Must cancel at least 24 hours before check-in
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
                await _notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = reservation.UserId,
                    ReservationId = reservation.Id,
                    Type = "reservation_cancelled",
                    Message = $"Your reservation {reservation.ConfirmationNumber} has been cancelled."
                });
            }
        }
        catch
        {
            // ignore notification failures
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



    // public async Task<ReservationResponse> CreateWithPaymentIntentAsync(ReservationUpsertRequest request)
    // {
    //     var entity = new Reservation();
    //     MapInsertToEntity(entity, request);

    //     await BeforeInsert(entity, request);

    //     _context.Set<Reservation>().Add(entity);
    //     await _context.SaveChangesAsync();

    //     return MapToResponse(entity);
    // }


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
            var days = nights + 1; // 3 nights = 4 days
            return addOn.Price * item.Quantity * days;
        }
        if (perGuestPerNight) return addOn.Price * item.Quantity * nights * guests;

        // PerStay
        return addOn.Price * item.Quantity;
    }

    private async Task SendReservationCreatedNotificationAsync(Reservation entity)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(entity.UserId))
            {
                await _notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = entity.UserId,
                    ReservationId = entity.Id,
                    Type = "reservation_created",
                    Message = $"Your reservation {entity.ConfirmationNumber} has been created."
                });
            }
        }
        catch
        {
            // ignore notification failure
        }
    }

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

        // AmountPaid / Total from latest payment if available
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
