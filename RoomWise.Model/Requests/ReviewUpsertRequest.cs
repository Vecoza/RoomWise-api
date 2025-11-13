using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class ReviewUpsertRequest
{
    [Required] public int HotelId { get; set; }

    [Required, Range(1,5)]
    public short Rating { get; set; } // 1-5

    [MaxLength(120)]
    public string? Title { get; set; }

    public string? Body { get; set; }

    
    public string? UserId { get; set; }
}