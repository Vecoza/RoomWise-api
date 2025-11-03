using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class ReviewUpsertRequest
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    public short Rating { get; set; }

    [MaxLength(120)]
    public string? Title { get; set; }

    public string? Body { get; set; }
}