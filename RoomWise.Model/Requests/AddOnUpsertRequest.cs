using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class AddOnUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Range(0, 9999999)]
    public decimal Price { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;
}