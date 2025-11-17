using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class ReservationAddOn
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    [ForeignKey(nameof(AddOn))]
    public int AddOnId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal LineTotal { get; set; }

    public virtual Reservation? Reservation { get; set; }
    public virtual AddOn? AddOn { get; set; }
}