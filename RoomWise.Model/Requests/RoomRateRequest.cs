using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomRateRequest
{
    [Required] 
    public int RoomTypeId { get; set; }
    [Required] 
    public DateTime StartDate { get; set; }
    [Required] 
    public DateTime EndDate { get; set; }
    
    [Range(0, 99999999)] 
    public decimal Price { get; set; }
    
    
    [Required, StringLength(3, MinimumLength = 3)] 
    public string Currency { get; set; } = "EUR";

 
    
}