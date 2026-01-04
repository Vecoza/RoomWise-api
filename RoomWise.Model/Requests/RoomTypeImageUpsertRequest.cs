using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomTypeImageUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; } = 0;
}
