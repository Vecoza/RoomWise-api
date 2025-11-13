namespace RoomWise.Model.SearchObject;

public class PaymentSearchObject : BaseSearchObject
{
    public int? ReservationId { get; set; }
    public string? Status { get; set; }    
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    
}