namespace RoomWise.Model;

public class Hotel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

  
    public string AddressLine { get; set; } = string.Empty;

    public string? Email { get; set; } = string.Empty;

    public string? Website { get; set; } = string.Empty;

  
    public int CityId { get; set; }

    public decimal Rating { get; set; }

    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    
    public virtual City City { get; set; } = null!;
    public virtual ICollection<PhoneContact> PhoneContacts { get; set; } = new List<PhoneContact>();
    public virtual ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();
    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    public virtual ICollection<HotelFacility> HotelFacilities { get; set; } = new List<HotelFacility>();
    public virtual ICollection<HotelTag> HotelTags { get; set; } = new List<HotelTag>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    
    public virtual ICollection<AddOn> AddOns { get; set; } = new List<AddOn>();

}