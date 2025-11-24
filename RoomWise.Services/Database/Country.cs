using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Country
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "char(2)"), MaxLength(2)]
    public string? Iso2 { get; set; } = "BA";

    public virtual ICollection<City> Cities { get; set; } = new List<City>();

}