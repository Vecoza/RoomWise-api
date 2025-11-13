using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class LoyaltyPoint
{
    [Key] 
    public long Id { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;

    public int Delta { get; set; } // +earn / -spend

    [MaxLength(200)]
    public string Reason { get; set; } = "Payment";

    public int? ReservationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser User { get; set; } = null!;
}