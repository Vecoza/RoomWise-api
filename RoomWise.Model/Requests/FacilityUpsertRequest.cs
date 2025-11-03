using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class FacilityUpsertRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
}