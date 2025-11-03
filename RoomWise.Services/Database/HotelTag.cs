using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class HotelTag
{
    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [ForeignKey(nameof(Tag))]
    public int TagId { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}