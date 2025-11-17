namespace RoomWise.Model.Responses;

public class AddOnResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PricingModel { get; set; } = "PerNight";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; }
}