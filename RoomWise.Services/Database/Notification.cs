using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    public int? ReservationId { get; set; }

    [Required, MaxLength(40)]
    public string Type { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual Reservation? Reservation { get; set; }
}