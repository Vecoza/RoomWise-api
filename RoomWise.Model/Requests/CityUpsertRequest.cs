using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class CityUpsertRequest
{
    [Required]
    public int CountryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";
}