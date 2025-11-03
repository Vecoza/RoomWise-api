namespace RoomWise.Model.SearchObject;

public class RoomTypeSearchObject : BaseSearchObject
{
    public int? HotelId { get; set; }
    public string? Name { get; set; }
    
    public string? BedType { get; set; }
    
    public int? MinCapacity { get; set; }
    
    public int? MaxCapacity { get; set; }
    
}