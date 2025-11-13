namespace RoomWise.Model.SearchObject;

public class ReviewSearchObject : BaseSearchObject
{
    public int? HotelId { get; set; }
    public string? UserId { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
}