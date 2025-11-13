using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class PromotionUpsertRequest
{
    public int? HotelId { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercent { get; set; }

    [Range(0, 9999999)]
    public decimal? DiscountFixed { get; set; }

    [Range(0, 365)]
    public int MinNights { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
    
    
}