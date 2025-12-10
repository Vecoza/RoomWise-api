using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class Review
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Hotel))]
    public int HotelId { get; set; }

    [ForeignKey(nameof(Reservation))]
    public int ReservationId { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;


    [Required]
    public short Rating { get; set; } // 1-5

    [MaxLength(120)]
    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual Reservation Reservation { get; set; } = null!;
    public virtual AppUser? User { get; set; }
}