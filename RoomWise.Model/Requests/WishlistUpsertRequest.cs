using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class WishlistUpsertRequest
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public int HotelId { get; set; }
}