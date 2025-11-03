using RoomWise.Model;
using RoomWise.Model.SearchObject;

namespace RoomWise.Services.Services;

public class DummyHotelService
{
    /*public List<Hotel> Get(HotelSearchObject? search)
    {
        List<Hotel> hotels = new List<Hotel>();

        hotels.Add(new Hotel()
        {
            Name = "Hotel",
            Description = "Neki"
        });

        var queryable = hotels.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search?.Description))
        {
            queryable = queryable.Where(s => s.Description == search.Description);
        }

        if (!string.IsNullOrWhiteSpace(search?.DescriptionGTE))
        {
            queryable = queryable.Where(s => s.Description.StartsWith(search.DescriptionGTE));
        }

        if (!string.IsNullOrWhiteSpace(search?.FTS))
        {
            queryable = queryable.Where(s => s.Description.Contains(search.FTS.ToLower()));
        }

        return queryable.ToList();
    }

    public Hotel GetById(int id)
    {
        return new Hotel()
        {
            Name = "Hotel",
            Description = "Neki"
        };
    }*/
}


