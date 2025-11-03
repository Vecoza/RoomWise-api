// RoomWise.Model/RoomType.cs
namespace RoomWise.Model;

public class RoomType
{
    public int Id { get; set; }

    // FK → Hotel.Id
    public int HotelId { get; set; }

    // e.g., “King bed, non-smoking”
    public string Name { get; set; } = null!;

    // e.g., “King”, “Twin”
    public string BedType { get; set; } = null!;

    // number of people
    public int Capacity { get; set; }

    public bool IsSmokingAllowed { get; set; } = false;

    // numeric(10,2)
    public decimal BasePrice { get; set; }

    // 3-letter ISO code, default "USD"
    public string Currency { get; set; } = "USD";

    // number of physical rooms of this type
    public int Stock { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    
    
    public virtual Hotel Hotel { get; set; } = null!;
    public virtual ICollection<RoomTypeImage> Images { get; set; } = new List<RoomTypeImage>();
    public virtual ICollection<RoomRate> Rates { get; set; } = new List<RoomRate>();
    public virtual ICollection<RoomTypeFacility> RoomTypeFacilities { get; set; } = new List<RoomTypeFacility>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<RoomAvailability> Availabilities { get; set; } = new List<RoomAvailability>();

}