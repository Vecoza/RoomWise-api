// RoomWise.Services/Services/ReservationService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Stripe;

namespace RoomWise.Services.Services;

public sealed class ReservationService
    : BaseCRUDService<ReservationResponse, ReservationSearchObject, Reservation, ReservationUpsertRequest, ReservationUpsertRequest>,
      IReservationService
{
    private readonly DbContext _context;
    private readonly IRoomAvailabilityService _availability;

    public ReservationService(DbContext context, IMapper mapper, IRoomAvailabilityService availability)
        : base(context, mapper)
    {
        _context = context;
        _availability = availability;
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

    
    public  async Task<ReservationResponse> InsertAsync(ReservationUpsertRequest request)
    {
     
        var checkIn  = request.CheckIn.Date;
        var checkOut = request.CheckOut.Date;

        if (checkOut <= checkIn) throw new ArgumentException("CheckOut must be after CheckIn.");
        if (request.Guests <= 0)  throw new ArgumentException("Guests must be >= 1.");

        var roomType = await _context.Set<RoomType>().FindAsync(request.RoomTypeId);
        if (roomType is null) throw new InvalidOperationException("RoomType not found.");
        if (request.Guests > roomType.Capacity)
            throw new InvalidOperationException("Guests exceed room type capacity.");

        var nights = (checkOut - checkIn).Days;
        if (nights <= 0) throw new ArgumentException("Invalid date range.");

      
        var nightly = await _context.Set<RoomRate>()
            .Where(r => r.RoomTypeId == request.RoomTypeId
                     && r.StartDate <= checkIn
                     && r.EndDate   >= checkOut)
            .Select(r => r.Price)
            .DefaultIfEmpty(roomType.BasePrice)
            .MinAsync();

        var entity = new Reservation();
        MapInsertToEntity(entity, request);
        entity.PublicId           = entity.PublicId == Guid.Empty ? Guid.NewGuid() : entity.PublicId;
        entity.CheckIn            = checkIn;
        entity.CheckOut           = checkOut;
        entity.Subtotal           = nightly * nights;
        entity.Currency           = roomType.Currency;
        entity.CreatedAt          = DateTime.UtcNow;
        entity.Status             = "Pending";
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
                var dayEnd   = dayStart.AddDays(1);

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
        await tx.CommitAsync();

        return MapToResponse(entity);
    }

   
    public override Task<ReservationResponse?> UpdateAsync(int id, ReservationUpsertRequest request)
        => base.UpdateAsync(id, request);

   
    public async Task<bool> CancelAsync(Guid publicId, Guid requestedByUserId)
    {
        var reservation = await _context.Set<Reservation>()
            .FirstOrDefaultAsync(r => r.PublicId == publicId);

        if (reservation is null) return false;

     
        if (reservation.Status == "Cancelled") return true;
        if (reservation.Status != "Pending" && reservation.Status != "Confirmed") return false;
        if (DateTime.UtcNow.Date >= reservation.CheckIn.Date) return false;

        using var tx = await _context.Database.BeginTransactionAsync();

        reservation.Status      = "Cancelled";
        reservation.CancelledAt = DateTime.UtcNow;


        var hasRoomAvailability = _context.Model.FindEntityType(typeof(RoomAvailability)) is not null;
        if (hasRoomAvailability)
        {
            await _availability.RestoreRangeAsync(reservation.RoomTypeId, reservation.CheckIn.Date, reservation.CheckOut.Date);
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return true;
    }

   
    public async Task<PagedResult<ReservationResponse>> GetMyAsync(Guid userId, string? category)
    {
        var userIdStr = userId.ToString();
        var q = _context.Set<Reservation>().Where(r => r.UserId == userIdStr);

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
}
