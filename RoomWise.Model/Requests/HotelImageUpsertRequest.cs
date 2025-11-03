using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelImageUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required, MaxLength(400)]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; } = 0;
}