namespace RoomWise.Model.Responses;

public class PaymentMethodResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string StripePaymentMethodId { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public short? ExpMonth { get; set; }
    public short? ExpYear { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}