namespace RoomWise.Model.Responses;

public class WishlistResponse
{
    public int Id { get; set; }   
    public string UserId { get; set; } = null!;
    public int HotelId { get; set; }
    public DateTime CreatedAt { get; set; }
}