namespace RoomWise.Model.Responses;

public class HotelDetailsResponse
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string AddressLine { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public double Rating { get; set; }
	public string City { get; set; } = string.Empty;
	public IEnumerable<string> Amenities { get; set; } = Array.Empty<string>();
	public IEnumerable<string> Photos { get; set; } = Array.Empty<string>();
	public IEnumerable<AvailableRoomType> AvailableRoomTypes { get; set; } = Array.Empty<AvailableRoomType>();
	public IEnumerable<TagResponse> Tags { get; set; } = Array.Empty<TagResponse>();

	public List<AddOnResponse> AddOns { get; set; } = new();
}

public class AvailableRoomType
{
	public int RoomTypeId { get; set; }
	public string Name { get; set; } = string.Empty;
	public int Capacity { get; set; }
	public decimal NightlyPrice { get; set; }           // effective (may include promo)
	public decimal OriginalNightlyPrice { get; set; }    // pre-promo price
	public int RoomsLeft { get; set; }

	// TEST
	public string? ThumbnailUrl { get; set; }
	public List<string> ImageUrls { get; set; } = new();
	public string BedType { get; set; } = string.Empty;
	public bool IsSmokingAllowed { get; set; }

    // Promo info (optional)
    public string? PromotionTitle { get; set; }
    public decimal? PromotionDiscountPercent { get; set; }
    public decimal? PromotionDiscountFixed { get; set; }
    public DateTime? PromotionEndDate { get; set; }
}
