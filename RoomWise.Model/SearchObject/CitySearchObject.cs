using RoomWise.Model.SearchObject;

namespace RoomWise.Model.SearchObject;

public class CitySearchObject : BaseSearchObject
{
    public int? CountryId { get; set; }
    public string? Name { get; set; }
}
