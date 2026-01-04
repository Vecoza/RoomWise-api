using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class RoomTypeImage
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(RoomType))]
    public int RoomTypeId { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string Url { get; set; } = null!;

    public int SortOrder { get; set; } = 0;

    public virtual RoomType RoomType { get; set; } = null!;
}
