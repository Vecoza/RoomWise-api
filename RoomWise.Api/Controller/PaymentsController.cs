
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly IHostEnvironment _env;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService payments,
        IOptions<StripeOptions> stripeOptions,
        IHostEnvironment env,
        ILogger<PaymentsController> logger)
        : base(payments)
    {
        _payments = payments;
        _stripeOptions = stripeOptions.Value;
        _env = env;
        _logger = logger;
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
            if (_env.IsDevelopment() && _stripeOptions.DisableWebhookSignature)
            {
                _logger.LogWarning("Stripe webhook signature validation disabled (Development).");
                stripeEvent = EventUtility.ParseEvent(json, throwOnApiVersionMismatch: false);
            }
            else if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            {
                return BadRequest();
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    sigHeader,
                    _stripeOptions.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature validation failed.");
            return BadRequest(); 
        }

        await _payments.HandleWebhookAsync(stripeEvent);
        return Ok();
    }
}
