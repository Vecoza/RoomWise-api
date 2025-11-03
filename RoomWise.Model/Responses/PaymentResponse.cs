namespace RoomWise.Model.Responses;

public class PaymentResponse
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "";
    public string Provider { get; set; } = "";
    public string? PaymentIntentId { get; set; }
    public string? ChargeId { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }
    public DateTime CreatedAt { get; set; }
}