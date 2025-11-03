namespace RoomWise.Model.Responses;

public class ReservationAddOnResponse
{
    public int ReservationId { get; set; }
    public int AddOnId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}