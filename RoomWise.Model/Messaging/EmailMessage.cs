namespace RoomWise.Model.Messaging;

public sealed class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public string? UserId { get; set; }
}
