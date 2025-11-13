namespace RoomWise.Model.SearchObject;

public class RoomAvailabilitySearchObject : BaseSearchObject
{
    public int? RoomTypeId { get; set; }
    public DateTime? From { get; set; } // inclusive
    public DateTime? To { get; set; }   // exclusive
}