using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelFacilityUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public int FacilityId { get; set; }
}