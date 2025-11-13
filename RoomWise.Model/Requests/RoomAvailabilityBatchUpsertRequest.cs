using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class RoomAvailabilityBatchUpsertRequest
{
    [Required]
    public int RoomTypeId { get; set; }

    // Option A: a continuous range (inclusive start, exclusive end)
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? Available { get; set; }

    // Option B: explicit per-date items
    public List<RoomAvailabilityUpsertRequest>? Items { get; set; }
}