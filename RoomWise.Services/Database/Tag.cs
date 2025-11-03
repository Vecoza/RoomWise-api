using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = null!;

    public virtual ICollection<HotelTag> HotelTags { get; set; } = new List<HotelTag>();

}