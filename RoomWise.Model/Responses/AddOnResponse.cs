namespace RoomWise.Model.Responses;

public class AddOnResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; }
}