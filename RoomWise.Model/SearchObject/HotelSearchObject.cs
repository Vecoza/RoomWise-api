

namespace RoomWise.Model.SearchObject;

public class HotelSearchObject : BaseSearchObject
{
	public int? CountryId { get; set; }
	public int? CityId { get; set; }

	public DateTime? CheckIn { get; set; }
	public DateTime? CheckOut { get; set; }

	public int? Guests { get; set; }

	public decimal? BudgetMin { get; set; }
	public decimal? BudgetMax { get; set; }

	public string? Query { get; set; }


	public int? TagId { get; set; }
	public string? TagName { get; set; }

	// Backwards-compat fields (still supported by service)
	public string? Name { get; set; }
	public decimal? MinRating { get; set; }
	public decimal? MaxRating { get; set; }
}