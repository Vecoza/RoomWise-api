using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Reservation
{
    [Key]
    public int Id { get; set; }

	public Guid PublicId { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;


    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [ForeignKey(nameof(RoomType))]
    public int RoomTypeId { get; set; }

    [Required, MaxLength(20)]
    public string ConfirmationNumber { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime CheckIn { get; set; }

    [Column(TypeName = "date")]
    public DateTime CheckOut { get; set; }

    public int Guests { get; set; } = 1;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

    [Column(TypeName = "numeric(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal TaxesAndFees { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal ServiceFee { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "char(3)"), MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public int? PromotionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual Hotel Hotel { get; set; } = null!;
    public virtual RoomType RoomType { get; set; } = null!;
    public virtual Promotion? Promotion { get; set; }
    public virtual ICollection<ReservationAddOn> AddOns { get; set; } = new List<ReservationAddOn>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    
}