using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class WishlistCreateRequest
{
    [Required]
    public int HotelId { get; set; }
}