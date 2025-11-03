using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class PhoneContactUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [MaxLength(50)]
    public string? Label { get; set; }

    [Required, MaxLength(40)]
    public string PhoneNumber { get; set; } = "";
}