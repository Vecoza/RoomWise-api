namespace RoomWise.Model.SearchObject;

public class ReservationSearchObject : BaseSearchObject
{
    public string? UserId { get; set; }
    public int? HotelId { get; set; }
    public int? RoomTypeId { get; set; }
    public string? Status { get; set; }          // Pending, Confirmed, Cancelled, Completed
    public DateTime? FromCheckIn { get; set; }
    public DateTime? ToCheckIn { get; set; }
    
    
}