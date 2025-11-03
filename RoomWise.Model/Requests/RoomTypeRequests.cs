using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomTypeUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string BedType { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Capacity { get; set; }

    public bool IsSmokingAllowed { get; set; } = false;
    
    public decimal BasePrice { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; } 
    
    
    public int Stock { get; set; } = 0;
}