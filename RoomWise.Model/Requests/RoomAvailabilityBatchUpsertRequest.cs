using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomAvailabilityBatchUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }


    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? Available { get; set; }

    public List<RoomAvailabilityUpsertRequest>? Items { get; set; }
}