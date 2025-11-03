using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomTypeImageUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }

    [Required, MaxLength(400)]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; } = 0;
}