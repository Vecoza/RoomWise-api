using RoomWise.Model.Requests;
using RoomWise.Model.Responses;
using RoomWise.Model.SearchObject;
using Stripe;

namespace RoomWise.Services.Interface;


public interface IPaymentService : IService<PaymentResponse, PaymentSearchObject>
{

    Task<(PaymentResponse payment, string clientSecret)> CreatePaymentIntentAsync(PaymentCreateRequest request);
    
    Task HandleWebhookAsync(Event stripeEvent);
}