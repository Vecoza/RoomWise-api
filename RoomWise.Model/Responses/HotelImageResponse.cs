namespace RoomWise.Model.Responses;

public class HotelImageResponse
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}