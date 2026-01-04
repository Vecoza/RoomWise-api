using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelImageUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; } = 0;
}
