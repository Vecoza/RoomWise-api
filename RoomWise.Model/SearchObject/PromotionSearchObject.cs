namespace RoomWise.Model.SearchObject;

public class PromotionSearchObject : BaseSearchObject
{
    public int? HotelId { get; set; }
    public bool? ActiveOnly { get; set; }
    public DateTime? From { get; set; } 
    public DateTime? To   { get; set; } 
    public int? MinNightsGte { get; set; }
}
