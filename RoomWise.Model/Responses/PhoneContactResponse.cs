namespace RoomWise.Model.Responses;

public class PhoneContactResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string? Label { get; set; }
    public string PhoneNumber { get; set; } = "";
}