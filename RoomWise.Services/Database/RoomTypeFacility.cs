using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class RoomTypeFacility
{
    [ForeignKey(nameof(RoomType))]
    public int RoomTypeId { get; set; }

    [ForeignKey(nameof(Facility))]
    public int FacilityId { get; set; }

    public virtual RoomType RoomType { get; set; } = null!;
    public virtual Facility Facility { get; set; } = null!;
}