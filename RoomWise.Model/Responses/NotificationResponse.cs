namespace RoomWise.Model.Responses;

public class NotificationResponse
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int? ReservationId { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}