using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Wishlist
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual Hotel Hotel { get; set; } = null!;
}