using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class PromotionPreviewRequest
{
    public int? HotelId { get; set; } 
    
    [Required] 
    public DateTime CheckIn { get; set; }
    
    [Required] 
    public DateTime CheckOut { get; set; }
    
    [Range(0.01, 9999999)] 
    public decimal BaseNightly { get; set; }
}