using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;


public sealed class ReservationsController 
    : BaseCRUDController<ReservationResponse, ReservationSearchObject, ReservationUpsertRequest, ReservationUpsertRequest>
{
    private readonly IReservationService _reservations;
    private readonly IPaymentService _payments;

    public ReservationsController(IReservationService reservations, IPaymentService payments)
        : base(reservations)
    {
        _reservations = reservations;
        _payments = payments;
    }


    [HttpGet("/api/reservations/my")]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> My([FromQuery] string? status)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {
            return Forbid();
        }
        var result = await _reservations.GetMyAsync(userId, status);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    /*[Authorize]*/
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {

            return Forbid();
        }

        var result = await _reservations.CancelAsync(id, userId);
        if (!result) return NotFound();
        return NoContent();
    }
    
 
    [HttpPost("with-payment-intent")]
    public async Task<ActionResult<object>> CreateWithPaymentIntent(
        [FromBody] ReservationUpsertRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken ct = default)
    {
  
        var reservation = await _reservations.InsertAsync(request);

        
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

       (PaymentResponse payment, string clientSecret) = await _payments.CreatePaymentIntentAsync(
            new PaymentCreateRequest
            {
                ReservationId = reservation.Id,
                Amount = reservation.Subtotal,
                Currency = reservation.Currency,
                Provider = "Stripe"
            }
        );
        return Ok(new { reservation, payment, clientSecret });
    }


    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<ReservationResponse>> GetByPublicId(Guid publicId, CancellationToken ct = default)
    {
        var res = await _reservations.GetByPublicIdAsync(publicId, ct);
        return res is null ? NotFound() : Ok(res);
    }


    [HttpPost("/api/reservations/{id:guid}/cancel")]
    public Task<IActionResult> CancelAlias(Guid id) => Cancel(id);
}
