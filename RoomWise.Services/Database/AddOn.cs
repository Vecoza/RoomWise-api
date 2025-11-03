using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class AddOn
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "char(3)"), MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    public virtual Hotel Hotel { get; set; } = null!;
    public virtual ICollection<ReservationAddOn> ReservationAddOns { get; set; } = new List<ReservationAddOn>();

}