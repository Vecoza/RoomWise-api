using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Services.Interface;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/me/payment-methods")]
[Authorize]
public sealed class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethods;

    public PaymentMethodsController(IPaymentMethodService paymentMethods)
        => _paymentMethods = paymentMethods;

    private string? GetUserIdOrForbid(out ActionResult? forbid)
    {
        forbid = null;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            forbid = Forbid();
            return null;
        }

        return userId;
    }

    // GET /api/me/payment-methods?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentMethodResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var result = await _paymentMethods.GetMineAsync(userId!, page, pageSize, ct);
        return Ok(result);
    }

    // POST /api/me/payment-methods
    // Frontend sends StripePaymentMethodId + display details.
    [HttpPost]
    public async Task<ActionResult<PaymentMethodResponse>> Create(
        [FromBody] PaymentMethodUpsertRequest req,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var created = await _paymentMethods.CreateAsync(userId!, req, ct);
        return Ok(created);
    }

    // DELETE /api/me/payment-methods/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var userId = GetUserIdOrForbid(out var forbid);
        if (forbid is not null) return forbid;

        var ok = await _paymentMethods.DeleteAsync(userId!, id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
