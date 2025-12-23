using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RoomWise.Model;

namespace RoomWise.Model;

public class HotelAdmin
{
    [Key]
    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [Required]
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Hotel? Hotel { get; set; }
    public virtual AppUser? User { get; set; }
}
