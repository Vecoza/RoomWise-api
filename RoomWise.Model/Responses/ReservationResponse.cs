namespace RoomWise.Model.Responses;

public class ReservationResponse
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int HotelId { get; set; }
    public int RoomTypeId { get; set; }
    public string ConfirmationNumber { get; set; } = "";
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; }
    public string Status { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal TaxesAndFees { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public int? PromotionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}