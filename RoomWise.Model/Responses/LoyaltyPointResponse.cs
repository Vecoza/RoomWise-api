namespace RoomWise.Model.Responses;

public class LoyaltyPointResponse
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public int? ReservationId { get; set; }
    public DateTime CreatedAt { get; set; }
}