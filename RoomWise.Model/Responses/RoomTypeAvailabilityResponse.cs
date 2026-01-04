namespace RoomWise.Model.Responses;

public sealed class RoomTypeAvailabilityResponse
{
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime Date { get; set; }
}
