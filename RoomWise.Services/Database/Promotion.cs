using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Promotion
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int? HotelId { get; set; } // null = global

    [Required, MaxLength(120)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "numeric(5,2)")]
    public decimal? DiscountPercent { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal? DiscountFixed { get; set; }

    public int MinNights { get; set; } = 0;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
    
    public virtual Hotel? Hotel { get; set; }
}