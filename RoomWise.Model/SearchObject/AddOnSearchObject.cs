namespace RoomWise.Model.SearchObject;


public class AddOnSearchObject : BaseSearchObject
{
    public int? HotelId { get; set; }
    public bool? IsActive { get; set; }
    public int? ForcedHotelId { get; set; }
}
