using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;


public class HotelFacility
{
    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [ForeignKey(nameof(Facility))]
    public int FacilityId { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;
    public virtual Facility Facility { get; set; } = null!;
}