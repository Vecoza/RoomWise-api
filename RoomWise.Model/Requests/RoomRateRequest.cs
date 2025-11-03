using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomRateRequest
{
    [Required]
    public int RoomTypeId { get; set; } 
    
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
    
    public decimal Price { get; set; }
    
    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";
 
    
}