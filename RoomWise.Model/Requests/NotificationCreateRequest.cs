using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class NotificationCreateRequest
{
    [Required]
    public string UserId { get; set; }

    public int? ReservationId { get; set; }

    [Required, MaxLength(40)]
    public string Type { get; set; } = "";

    [Required]
    public string Message { get; set; } = "";
}