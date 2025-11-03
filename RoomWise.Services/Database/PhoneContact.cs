using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class PhoneContact
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [MaxLength(50)]
    public string? Label { get; set; }

    [Required, MaxLength(40)]
    public string PhoneNumber { get; set; } = null!;

    public virtual Hotel Hotel { get; set; } = null!;
}