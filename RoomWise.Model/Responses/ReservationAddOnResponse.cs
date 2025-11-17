namespace RoomWise.Model.Responses;

public class ReservationAddOnResponse
{
    public int AddOnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string PricingModel { get; set; } = string.Empty;
}