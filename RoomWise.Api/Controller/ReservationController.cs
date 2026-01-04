using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using RoomWise.Api.Auth;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
/*[Authorize(Roles = $"{AppRoles.Guest},{AppRoles.Administrator}")]*/
public class ReservationsController
    : BaseCRUDController<ReservationResponse, ReservationSearchObject, ReservationUpsertRequest, ReservationUpsertRequest>
{
    private readonly IReservationService _reservations;
    private readonly IPaymentService _payments;
    private readonly HotelAdminScope _scope;

    public ReservationsController(IReservationService reservations, IPaymentService payments, HotelAdminScope scope)
        : base(reservations)
    {
        _reservations = reservations;
        _payments = payments;
        _scope = scope;
    }


    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> My([FromQuery] string? status)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Forbid();

        var result = await _reservations.GetMyAsync(userId, status);
        return Ok(result);
    }


    // [HttpPost("{id:guid}/cancel")]
    // public async Task<IActionResult> Cancel(Guid id)
    // {
    //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
    //     if (string.IsNullOrWhiteSpace(userId))
    //         return Forbid();

    //     var result = await _reservations.CancelAsync(id, userId);
    //     if (!result) return NotFound();
    //     return NoContent();
    // }


    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _reservations.CancelAsync(id, ct);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<PagedResult<ReservationResponse>> Get([FromQuery] ReservationSearchObject? search = null)
    {
        return Filtered(async () => await base.Get(search));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet("arrivals")]
    public async Task<ActionResult<IReadOnlyList<ReservationArrivalResponse>>> Arrivals(
        [FromQuery] DateTime? date,
        CancellationToken ct = default)
    {
        var hotelId = await _scope.GetHotelIdAsync(ct);
        if (!hotelId.HasValue) return Forbid();

        _reservations.ForceHotelScope(hotelId.Value);

        var result = await _reservations.GetArrivalsAsync(date?.Date ?? DateTime.UtcNow.Date, ct);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<ReservationResponse?> GetById(int id)
    {
        return Filtered(async () => await base.GetById(id));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<ReservationResponse?> Update(int id, [FromBody] ReservationUpsertRequest req)
    {
        return Filtered(async () => await base.Update(id, req));
    }

    [Authorize(Roles = AppRoles.Administrator)]
    public override Task<bool> Delete(int id)
    {
        return Filtered(async () =>
        {
            await _reservations.CancelAsAdminAsync(id, CancellationToken.None);
            return true;
        });
    }

    private async Task<T> Filtered<T>(Func<Task<T>> action)
    {
        var hotelId = await _scope.GetHotelIdAsync();
        if (hotelId.HasValue) _reservations.ForceHotelScope(hotelId.Value);
        return await action();
    }



    [HttpPost("with-payment-intent")]
    public async Task<ActionResult<object>> CreateWithPaymentIntent(
        [FromBody] ReservationUpsertRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken ct = default)
    {

        var reservation = await _reservations.CreateAsync(request);


        var existing = await _reservations.FindActivePaymentAsync(reservation.Id);
        if (existing is not null)
        {
            var (existingPayment, existingClientSecret) = existing.Value;
            return Ok(new
            {
                reservation,
                payment = existingPayment,
                clientSecret = existingClientSecret
            });
        }

        var preRedeemTotal = reservation.Subtotal + reservation.TaxesAndFees + reservation.ServiceFee;
        var redeemApplied = (int)Math.Max(0m, preRedeemTotal - reservation.Total);

        (PaymentResponse payment, string clientSecret) = await _payments.CreatePaymentIntentAsync(
             new PaymentCreateRequest
             {
                 ReservationId = reservation.Id,
                 Amount = reservation.Total,
                 Currency = reservation.Currency,
                 Provider = "Stripe",
                 LoyaltyPointsToRedeem = redeemApplied
             }
         );

        var refreshedReservation = await _reservations.GetByIdAsync(reservation.Id) ?? reservation;

        refreshedReservation.Total = payment.Amount;


        if (payment.Amount > 0 && string.IsNullOrWhiteSpace(clientSecret))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Failed to create payment intent.",
                amount = payment.Amount
            });
        }

        return Ok(new { reservation = refreshedReservation, payment, clientSecret });
    }


    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ReservationResponse>> GetByPublicId(Guid publicId, CancellationToken ct = default)
    {
        var res = await _reservations.GetByPublicIdAsync(publicId, ct);
        return res is null ? NotFound() : Ok(res);
    }


    /*[HttpPost("/api/reservations/{id:guid}/cancel")]
    public Task<IActionResult> CancelAlias(Guid id) => Cancel(id);*/
}
