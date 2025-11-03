using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class RoomRate
{
    public int Id { get; set; } 

    // FK → RoomType.Id 
    public int RoomTypeId { get; set; } 
    
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";
    
    public RoomType RoomType { get; set; } = null!;
}