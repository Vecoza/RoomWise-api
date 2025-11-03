using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomAvailabilityUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Range(0, 100000)]
    public int Available { get; set; }
}