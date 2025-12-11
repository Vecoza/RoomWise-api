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

    private const int StatusMaxLength = 20;

    public PaymentService(
        DbContext context,
        IMapper mapper,
        ILoyaltyService loyalty,
        INotificationService notifications)
        : base(context, mapper)
    {
        _context = context;
        _loyalty = loyalty;
        _notifications = notifications;


        _piService = new PaymentIntentService();
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

    public async Task<(PaymentResponse payment, string clientSecret)>
        CreatePaymentIntentAsync(PaymentCreateRequest request)
    {
        var reservation = await _context.Set<Reservation>()
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId);

        if (reservation is null)
            throw new InvalidOperationException("Reservation not found.");

        var currency = (request.Currency ?? reservation.Currency ?? "EUR").ToLowerInvariant();

        var baseAmount = request.Amount > 0 ? request.Amount : reservation.Total;
        if (baseAmount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        var balance = await _loyalty.GetBalanceAsync(reservation.UserId);
        var requestedRedeem = Math.Max(0, request.LoyaltyPointsToRedeem ?? 0);
        var redeemPoints = Math.Min(requestedRedeem, balance);
        // cap redemption so we don't exceed the reservation amount
        redeemPoints = (int)Math.Min(redeemPoints, Math.Floor(baseAmount));

        var amount = baseAmount - redeemPoints;
        if (amount <= 0)
        {
            // Fully covered by loyalty: no Stripe PaymentIntent needed
            var loyaltyPayment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = 0,
                Currency = currency.ToUpperInvariant(),
                Provider = "Loyalty",
                Status = "Succeeded",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Payment>().Add(loyaltyPayment);
            await _context.SaveChangesAsync();

            if (redeemPoints > 0)
            {
                await _loyalty.AddAsync(
                    userId: reservation.UserId,
                    delta: -redeemPoints,
                    reason: $"Redeem {redeemPoints} points for reservation {reservation.Id}",
                    reservationId: reservation.Id);
            }

            if (reservation.Status is "Pending" or "RequiresAction")
            {
                reservation.Status = "Confirmed";
                await _context.SaveChangesAsync();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(reservation.UserId))
                {
                    await _notifications.CreateAsync(new NotificationCreateRequest
                    {
                        UserId = reservation.UserId,
                        ReservationId = reservation.Id,
                        Type = "payment_succeeded",
                        Message =
                            $"Payment covered by loyalty points. Reservation {reservation.ConfirmationNumber} is confirmed."
                    });
                }
            }
            catch
            {
                // ignore
            }

            return (MapToResponse(loyaltyPayment), string.Empty);
        }


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

        var paymentEntity = new Payment
        {
            ReservationId = reservation.Id,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Provider = "Stripe",
            Status = NormalizeStatus("requires_payment_method"),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Payment>().Add(paymentEntity);
        await _context.SaveChangesAsync();

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero),
            Currency = currency,
            AutomaticPaymentMethods = savedMethod is null
            ? new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            : null,
            PaymentMethod = savedMethod?.StripePaymentMethodId,
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = paymentEntity.Id.ToString(),
                ["reservationId"] = reservation.Id.ToString(),
                ["redeemPoints"] = redeemPoints.ToString()
            }
        };

        var pi = await _piService.CreateAsync(options);

        paymentEntity.PaymentIntentId = pi.Id;
        paymentEntity.Status = NormalizeStatus(pi.Status);
        await _context.SaveChangesAsync();

        return (MapToResponse(paymentEntity), pi.ClientSecret ?? string.Empty);
    }

    public async Task HandleWebhookAsync(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await UpdatePaymentFromIntentAsync((PaymentIntent)stripeEvent.Data.Object, "Succeeded");
                break;

            case "payment_intent.payment_failed":
                await UpdatePaymentFromIntentAsync((PaymentIntent)stripeEvent.Data.Object, "Failed");
                break;

            case "payment_intent.requires_action":
            case "payment_intent.processing":
            case "payment_intent.canceled":
                var pi = (PaymentIntent)stripeEvent.Data.Object;
                await UpdatePaymentFromIntentAsync(pi, pi.Status);
                break;

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
            payment = await _context.Set<Payment>()
                .FirstOrDefaultAsync(p => p.Id == paymentIdFromMeta);
        }

        if (payment is null) return;

        payment.Status = NormalizeStatus(finalStatus);

        if (!string.IsNullOrWhiteSpace(pi.LatestChargeId))
            payment.ChargeId = pi.LatestChargeId;

        await _context.SaveChangesAsync();

        if (!string.Equals(finalStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
            return;

        var reservation = await _context.Set<Reservation>()
            .FirstOrDefaultAsync(r => r.Id == payment.ReservationId);

        if (reservation is null) return;

        // Apply loyalty redemption only after success
        if (pi.Metadata != null && pi.Metadata.TryGetValue("redeemPoints", out var redeemStr)
            && int.TryParse(redeemStr, out var redeemPoints) && redeemPoints > 0)
        {
            var balance = await _loyalty.GetBalanceAsync(reservation.UserId);
            if (balance < redeemPoints)
            {
                // insufficient balance at settle time; skip redemption
                redeemPoints = 0;
            }
            if (redeemPoints > 0)
            {
                await _loyalty.AddAsync(
                    userId: reservation.UserId,
                    delta: -redeemPoints,
                    reason: $"Redeem {redeemPoints} points for reservation {reservation.Id}",
                    reservationId: reservation.Id);
            }
        }

        if (reservation.Status is "Pending" or "RequiresAction")
        {
            reservation.Status = "Confirmed";
            await _context.SaveChangesAsync();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(reservation.UserId))
            {
                await _notifications.CreateAsync(new NotificationCreateRequest
                {
                    UserId = reservation.UserId,
                    ReservationId = reservation.Id,
                    Type = "payment_succeeded",
                    Message =
                        $"Payment {payment.Amount} {payment.Currency} succeeded. Reservation {reservation.ConfirmationNumber} is confirmed."
                });
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = status switch
        {
            "requires_payment_method" => "RequiresMethod",
            "requires_confirmation" => "RequiresConfirm",
            "requires_action" => "RequiresAction",
            "processing" => "Processing",
            "canceled" => "Canceled",
            "succeeded" => "Succeeded",
            _ => status
        };

        return normalized.Length > StatusMaxLength
            ? normalized[..StatusMaxLength]
            : normalized;
    }

}
