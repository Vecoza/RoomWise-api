using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Facility
{
   
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = null!; // "pool", "wifi", ...

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public virtual ICollection<HotelFacility> HotelFacilities { get; set; } = new List<HotelFacility>();
        public virtual ICollection<RoomTypeFacility> RoomTypeFacilities { get; set; } = new List<RoomTypeFacility>();
    
}