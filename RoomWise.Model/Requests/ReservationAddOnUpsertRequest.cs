using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class ReservationAddOnUpsertRequest
{
    [Required]
    public int ReservationId { get; set; }

    [Required]
    public int AddOnId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(0, 9999999)]
    public decimal UnitPrice { get; set; }
}