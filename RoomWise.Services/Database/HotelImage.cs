using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class HotelImage
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [Required, MaxLength(400)]
    public string Url { get; set; } = null!;

    public int SortOrder { get; set; } = 0;

    public virtual Hotel Hotel { get; set; } = null!;
}