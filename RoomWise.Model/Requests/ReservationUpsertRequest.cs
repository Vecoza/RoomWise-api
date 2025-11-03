using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class ReservationUpsertRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public int HotelId { get; set; }

    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    [Required]
    public DateTime CheckOut { get; set; }

    [Range(1, 100)]
    public int Guests { get; set; } = 1;

    public int? PromotionId { get; set; }
}