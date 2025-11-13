
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RoomWise.Api.Options;
using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using RoomWise.Services.Interface;
using Stripe;

namespace RoomWise.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : BaseController<PaymentResponse, PaymentSearchObject>
{
    private readonly IPaymentService _payments;
    private readonly StripeOptions _stripeOptions;

    public PaymentsController(IPaymentService payments, IOptions<StripeOptions> stripeOptions)
        : base(payments)
    {
        _payments = payments;
        _stripeOptions = stripeOptions.Value;
    }
    
    [HttpPost("intent")]
    public async Task<ActionResult<object>> CreateIntent([FromBody] PaymentCreateRequest request)
    {
        var (payment, clientSecret) = await _payments.CreatePaymentIntentAsync(request);
        return Ok(new
        {
            payment,
            clientSecret
        });
    }


    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
   
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var sigHeader = Request.Headers["Stripe-Signature"];
        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, sigHeader, _stripeOptions.WebhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest(); 
        }

        await _payments.HandleWebhookAsync(stripeEvent);
        return Ok();
    }
}