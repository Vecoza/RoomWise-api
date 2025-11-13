using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Wishlist
{
    [Key]                              
    public int Id { get; set; }
    
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;


    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual Hotel Hotel { get; set; } = null!;
}