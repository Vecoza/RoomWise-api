namespace RoomWise.Model.Responses;

public class HotelSearchItemResponse
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public decimal FromPrice { get; set; }
	public double Rating { get; set; }
	public int ReviewCount { get; set; }
	public string ThumbnailUrl { get; set; } = string.Empty;
	public bool HasAvailability { get; set; }
	public List<TagResponse> Tags { get; set; } = new();

	// Optional promotion info (used by hot-deals)
	public decimal? PromotionPrice { get; set; }
	public decimal? PromotionDiscountPercent { get; set; }
	public decimal? PromotionDiscountFixed { get; set; }
	public DateTime? PromotionEndDate { get; set; }
	public string? PromotionTitle { get; set; }
}
