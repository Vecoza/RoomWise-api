using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomTypeFacilityUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public int FacilityId { get; set; }
}