using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class AddOn
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

   
    [Required, MaxLength(20)]
    public string PricingModel { get; set; } = "PerNight";

   
    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;

    public virtual Hotel? Hotel { get; set; }
    public virtual ICollection<ReservationAddOn> ReservationAddOns { get; set; } = new List<ReservationAddOn>();
}