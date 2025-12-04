
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class City
{

    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Country))]
    public int CountryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;



    public virtual Country Country { get; set; } = null!;
    public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}