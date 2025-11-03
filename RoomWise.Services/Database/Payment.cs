using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "char(3)"), MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required, MaxLength(20)]
    public string Status { get; set; } = "RequiresAction"; // RequiresAction, Succeeded, Failed, Refunded

    [Required, MaxLength(30)]
    public string Provider { get; set; } = "Stripe";

    [MaxLength(100)]
    public string? PaymentIntentId { get; set; } // Stripe

    [MaxLength(100)]
    public string? ChargeId { get; set; }

    [MaxLength(20)]
    public string? CardBrand { get; set; }

    [Column(TypeName = "char(4)"), MaxLength(4)]
    public string? CardLast4 { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
}