namespace RoomWise.Model.Responses;

public class ReviewResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string UserId { get; set; } = null!;
    public short Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
}