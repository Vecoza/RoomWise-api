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
}

public class AvailableRoomType
{
	public int RoomTypeId { get; set; }
	public string Name { get; set; } = string.Empty;
	public int Capacity { get; set; }
	public decimal NightlyPrice { get; set; }
	public int RoomsLeft { get; set; }
}


