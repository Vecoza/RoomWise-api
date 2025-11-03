using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class OutboxMessageCreateRequest
{
    [Required, MaxLength(100)]
    public string AggregateType { get; set; } = "";

    public int? AggregateId { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = "";

    [Required]
    public string Payload { get; set; } = "";
}