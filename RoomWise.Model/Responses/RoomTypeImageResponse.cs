namespace RoomWise.Model.Responses;

public class RoomTypeImageResponse
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}