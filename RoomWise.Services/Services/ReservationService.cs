

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

    public ReservationService(
        DbContext context,
        IMapper mapper,
        IRoomAvailabilityService availability,
        INotificationService notifications,
        IHttpContextAccessor httpContextAccessor)
        : base(context, mapper)
    {
        _availability = availability;
        _notifications = notifications;
        _httpContextAccessor = httpContextAccessor;
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
            .MinAsync();

        // 2) Fallback to BasePrice in memory
        var nightly = dbRate ?? roomType.BasePrice;

        var roomTotal = nightly * nights;

        // 2) Add-ons
        decimal addOnsTotal = 0m;
        var addOnItems = request.AddOns ?? new List<ReservationAddOnItem>();

        if (addOnItems.Count > 0)
        {
            var addOnIds = addOnItems.Select(a => a.AddOnId).Distinct().ToList();

            var addOns = await _context.Set<AddOn>()
                .Where(a => addOnIds.Contains(a.Id))
                .ToListAsync();

            var addOnById = addOns.ToDictionary(a => a.Id);

            foreach (var item in addOnItems)
            {
                if (!addOnById.TryGetValue(item.AddOnId, out var addOn))
                    throw new InvalidOperationException($"Add-on {item.AddOnId} not found.");

                if (addOn.HotelId != roomType.HotelId)
                    throw new InvalidOperationException("Add-on does not belong to this hotel.");

                if (item.Quantity <= 0)
                    throw new ArgumentException("Add-on quantity must be >= 1.");

                decimal unitPrice;
                decimal lineTotal;

                var perNight = string.Equals(addOn.PricingModel, "PerNight", StringComparison.OrdinalIgnoreCase);

                unitPrice = addOn.Price;
                lineTotal = addOn.Price * item.Quantity * (perNight ? nights : 1);

                addOnsTotal += lineTotal;
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

        entity.PublicId = entity.PublicId == Guid.Empty ? Guid.NewGuid() : entity.PublicId;
        entity.CheckIn = checkIn;
        entity.CheckOut = checkOut;
        entity.Subtotal = roomTotal + addOnsTotal;
        entity.Total = entity.Subtotal + entity.TaxesAndFees + entity.ServiceFee;
        entity.Currency = roomType.Currency;
        entity.CreatedAt = DateTime.UtcNow;
        entity.Status = "Pending";
        entity.ConfirmationNumber = GenerateConfirmationNumber();



        using var tx = await _context.Database.BeginTransactionAsync();

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
                    .CountAsync();

                if (overlappingCount >= stock)
                    throw new InvalidOperationException($"No availability on {dayStart:yyyy-MM-dd}.");
            }
        }

        _context.Set<Reservation>().Add(entity);

        await _context.SaveChangesAsync();


        if (addOnItems.Count > 0)
        {
            var addOnIds = addOnItems.Select(a => a.AddOnId).Distinct().ToList();
            var addOns = await _context.Set<AddOn>()
                .Where(a => addOnIds.Contains(a.Id))
                .ToListAsync();

            var addOnById = addOns.ToDictionary(a => a.Id);

            var reservationAddOns = new List<ReservationAddOn>();

            foreach (var item in addOnItems)
            {
                if (!addOnById.TryGetValue(item.AddOnId, out var addOn))
                    continue; // already validated above but safe-guard

                var perNight = string.Equals(addOn.PricingModel, "PerNight", StringComparison.OrdinalIgnoreCase);
                var unitPrice = addOn.Price;
                var lineTotal = addOn.Price * item.Quantity * (perNight ? nights : 1);

                reservationAddOns.Add(new ReservationAddOn
                {
                    ReservationId = entity.Id,
                    AddOnId = addOn.Id,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });
            }

            if (reservationAddOns.Count > 0)
            {
                _context.Set<ReservationAddOn>().AddRange(reservationAddOns);
                await _context.SaveChangesAsync();
            }
        }

        await tx.CommitAsync();


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

        return MapToResponse(entity);
    }


    public override Task<ReservationResponse?> UpdateAsync(int id, ReservationUpsertRequest request)
        => base.UpdateAsync(id, request);

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



    public async Task<ReservationResponse> CreateWithPaymentIntentAsync(ReservationUpsertRequest request)
    {
        var entity = new Reservation();
        MapInsertToEntity(entity, request);

        await BeforeInsert(entity, request);

        _context.Set<Reservation>().Add(entity);
        await _context.SaveChangesAsync();

        return MapToResponse(entity);
    }


    private static string GenerateConfirmationNumber()
    {
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"RW-{DateTime.UtcNow:yyyyMMddHHmmss}-{token}";
    }

    public override async Task<ReservationResponse?> GetByIdAsync(int id) => await base.GetByIdAsync(id);

    public async Task<ReservationResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    {
        var entity = await _context.Set<Reservation>()
            .Include(r => r.Hotel).ThenInclude(h => h.City)
            .Include(r => r.Hotel).ThenInclude(h => h.Images)
            .Include(r => r.AddOns).ThenInclude(ra => ra.AddOn)
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
