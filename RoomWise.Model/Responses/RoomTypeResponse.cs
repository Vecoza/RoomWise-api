namespace RoomWise.Model.Responses;

public class RoomTypeResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string BedType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsSmokingAllowed { get; set; }

    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int Stock { get; set; }

    public DateTime CreatedAt { get; set; }


}