using RoomWise.Model.SearchObject;

namespace RoomWise.Model.SearchObject;

public class CountrySearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public string? Iso2 { get; set; }
}
