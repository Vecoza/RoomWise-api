

namespace RoomWise.Model.Responses;

public class RoomAvailabilityResponse
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public int Available { get; set; }
}