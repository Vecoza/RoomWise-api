using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class AddOnUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

 
    [Required, MaxLength(20)]
    public string PricingModel { get; set; } = "PerNight";

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;
}