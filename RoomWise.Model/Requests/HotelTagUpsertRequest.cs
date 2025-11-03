using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelTagUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public int TagId { get; set; }
}