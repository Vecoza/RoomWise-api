namespace RoomWise.Model.Responses;

public class ReviewResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public Guid UserId { get; set; }
    public short Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
}