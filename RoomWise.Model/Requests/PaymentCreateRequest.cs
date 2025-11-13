using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class PaymentCreateRequest
{
    [Required]
    public int ReservationId { get; set; }

    [Range(0, 999999999)]
    public decimal Amount { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(30)]
    public string Provider { get; set; } = "Stripe";
}