using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class PaymentMethod
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [Required, MaxLength(80)]
    public string StripeCustomerId { get; set; } = null!;

    [MaxLength(20)]
    public string? Brand { get; set; }

    [Column(TypeName = "char(4)"), MaxLength(4)]
    public string? Last4 { get; set; }

    public short? ExpMonth { get; set; }

    public short? ExpYear { get; set; }

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? User { get; set; }
}