using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Stripe;

using PaymentMethodEntity = RoomWise.Model.PaymentMethod;

namespace RoomWise.Services.Services;



public class PaymentService
    : BaseService<PaymentResponse, PaymentSearchObject, Payment>, IPaymentService
{
    private readonly DbContext _context;
    private readonly PaymentIntentService _piService;
    private readonly ILoyaltyService _loyalty;
    private readonly INotificationService _notifications;
    
    public PaymentService(DbContext context, IMapper mapper, ILoyaltyService loyalty, INotificationService notifications) : base(context, mapper)
    {
        _context = context;
        _piService = new PaymentIntentService();
        _loyalty = loyalty;
        _notifications = notifications;
    }

    protected override IQueryable<Payment> ApplyFilter(IQueryable<Payment> q, PaymentSearchObject s)
    {
        if (s.ReservationId.HasValue) q = q.Where(x => x.ReservationId == s.ReservationId.Value);
        if (!string.IsNullOrWhiteSpace(s.Status)) q = q.Where(x => x.Status == s.Status);
        if (s.From.HasValue) q = q.Where(x => x.CreatedAt >= s.From.Value);
        if (s.To.HasValue) q = q.Where(x => x.CreatedAt <= s.To.Value);
        if (!string.IsNullOrWhiteSpace(s.FTS))
            q = q.Where(x => x.Currency.Contains(s.FTS!) || x.Provider.Contains(s.FTS!));
        return q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
    }
    
  public async Task<(PaymentResponse payment, string clientSecret)> CreatePaymentIntentAsync(PaymentCreateRequest request)
{
    var reservation = await _context.Set<Reservation>()
        .FirstOrDefaultAsync(r => r.Id == request.ReservationId);
    if (reservation is null)
        throw new InvalidOperationException("Reservation not found.");

    var currency = (request.Currency ?? reservation.Currency ?? "EUR").ToLowerInvariant();

    var amount = request.Amount > 0 ? request.Amount : reservation.Total;
    if (amount <= 0)
        throw new InvalidOperationException("Payment amount must be greater than zero.");

    // 🔹 Saved payment method based on StripePaymentMethodId (string)
    PaymentMethodEntity? savedMethod = null;
    if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
    {
        savedMethod = await _context.Set<PaymentMethodEntity>()
            .FirstOrDefaultAsync(pm =>
                pm.StripePaymentMethodId == request.PaymentMethodId &&
                pm.UserId == reservation.UserId);

        if (savedMethod is null)
            throw new InvalidOperationException("Saved payment method not found for this user.");
    }

    var payment = new Payment
    {
        ReservationId = reservation.Id,
        Amount        = amount,
        Currency      = currency.ToUpperInvariant(),
        Provider      = "Stripe",
        Status        = "RequiresPaymentMethod",
        CreatedAt     = DateTime.UtcNow
    };

    _context.Set<Payment>().Add(payment);
    await _context.SaveChangesAsync();

    var options = new PaymentIntentCreateOptions
    {
        Amount   = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero),
        Currency = currency,

        // If no saved method → automatic payment methods (card element flow)
        AutomaticPaymentMethods = savedMethod is null
            ? new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            : null,

        // If a saved method exists → instruct Stripe to use it
        PaymentMethod = savedMethod?.StripePaymentMethodId,

        Metadata = new Dictionary<string, string>
        {
            ["paymentId"]     = payment.Id.ToString(),
            ["reservationId"] = reservation.Id.ToString()
        }
    };

    var pi = await _piService.CreateAsync(options);

    payment.PaymentIntentId = pi.Id;
    payment.Status          = pi.Status;
    await _context.SaveChangesAsync();

    return (MapToResponse(payment), pi.ClientSecret ?? string.Empty);
}

  
    public async Task HandleWebhookAsync(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
           case "payment_intent.succeeded":
            {
                var pi = (PaymentIntent)stripeEvent.Data.Object;
                await UpdatePaymentFromIntentAsync(pi, finalStatus: "Succeeded");
                break;
            }
            case "payment_intent.payment_failed":
            {
                var pi = (PaymentIntent)stripeEvent.Data.Object;
                await UpdatePaymentFromIntentAsync(pi, finalStatus: "Failed");
                break;
            }
            case "payment_intent.requires_action":
            case "payment_intent.processing":
            case "payment_intent.canceled":
            {
                var pi = (PaymentIntent)stripeEvent.Data.Object;
                await UpdatePaymentFromIntentAsync(pi, finalStatus: pi.Status);
                break;
            }
            default:
                break;
        }
    }

    private async Task UpdatePaymentFromIntentAsync(PaymentIntent pi, string finalStatus)
    {
        var payment = await _context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.PaymentIntentId == pi.Id);

        if (payment is null && pi.Metadata != null &&
            pi.Metadata.TryGetValue("paymentId", out var pid) &&
            int.TryParse(pid, out var paymentIdFromMeta))
        {
            payment = await _context.Set<Payment>().FirstOrDefaultAsync(p => p.Id == paymentIdFromMeta);
        }

        if (payment is null) return;

        payment.Status = finalStatus;

        if (!string.IsNullOrWhiteSpace(pi.LatestChargeId))
            payment.ChargeId = pi.LatestChargeId;

        await _context.SaveChangesAsync();

        if (string.Equals(finalStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            var reservation = await _context.Set<Reservation>()
                .FirstOrDefaultAsync(r => r.Id == payment.ReservationId);

            if (reservation is not null)
            {
                if (reservation.Status is "Pending" or "RequiresAction")
                {
                    reservation.Status = "Confirmed";
                    await _context.SaveChangesAsync();
                }

                var points = (int)Math.Floor(payment.Amount); 
                if (points > 0)
                {
                    await _loyalty.AddAsync(
                        userId: reservation.UserId,
                        delta: points,
                        reason: $"Payment {payment.Id} {payment.Currency}",
                        reservationId: reservation.Id);
                }
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(reservation.UserId))
                    {
                        await _notifications.CreateAsync(new NotificationCreateRequest
                        {
                            UserId        = reservation.UserId,
                            ReservationId = reservation.Id,
                            Type          = "payment_succeeded",
                            Message       =
                                $"Payment {payment.Amount} {payment.Currency} succeeded. Reservation {reservation.ConfirmationNumber} is confirmed."
                        });
                    }
                }
                catch
                {
                    // ignore 
                }
            }
        }
    }

}
