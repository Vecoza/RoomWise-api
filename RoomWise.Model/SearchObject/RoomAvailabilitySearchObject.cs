namespace RoomWise.Model.SearchObject;

public class RoomAvailabilitySearchObject : BaseSearchObject
{
    public int? RoomTypeId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? HotelId { get; set; }
}
