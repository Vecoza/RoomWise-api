using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class HotelUpsertRequest
{   
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public decimal Rating { get; set; } = 0;

    [Required]
    public int CityId { get; set; }
}


