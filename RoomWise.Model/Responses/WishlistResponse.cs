namespace RoomWise.Model.Responses;

public class WishlistResponse
{
    public Guid UserId { get; set; }
    public int HotelId { get; set; }
    public DateTime CreatedAt { get; set; }
}