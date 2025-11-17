using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelImageReorderRequest
{
    [Required]
    public List<(int Id, int SortOrder)> Items { get; set; } = new();

}