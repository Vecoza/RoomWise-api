namespace RoomWise.Model.Responses;

public class RoomRateResponse
{
    
    public int Id { get; set; }             
    public int RoomTypeId { get; set; }    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}