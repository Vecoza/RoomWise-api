using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class RoomAvailability
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(RoomType))]
    public int RoomTypeId { get; set; }

    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    public int Available { get; set; }

    public virtual RoomType RoomType { get; set; } = null!;
}