

namespace RoomWise.Model.SearchObject;

public class HotelSearchObject : BaseSearchObject
{
    public int? CityId { get; set; }
    public string? Name { get; set; }
    public decimal? MinRating { get; set; }
    public decimal? MaxRating { get; set; }
}