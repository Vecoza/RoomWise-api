namespace RoomWise.Model.Requests;

public class RoomTypeImageReorderRequest
{
    public List<RoomTypeImageReorderItem> Items { get; set; } = new();
}

public class RoomTypeImageReorderItem
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
}
