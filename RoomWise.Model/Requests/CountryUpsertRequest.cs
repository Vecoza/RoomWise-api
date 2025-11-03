using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class CountryUpsertRequest
{
    
    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [StringLength(2)]
    public string? Iso2 { get; set; }
}