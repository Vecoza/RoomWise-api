using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class ReservationAddOn
{
    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    [ForeignKey(nameof(AddOn))]
    public int AddOnId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "numeric(10,2)")]
    public decimal UnitPrice { get; set; }

    public virtual Reservation Reservation { get; set; } = null!;
    public virtual AddOn AddOn { get; set; } = null!;
}