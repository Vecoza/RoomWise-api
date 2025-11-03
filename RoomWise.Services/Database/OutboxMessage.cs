using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class OutboxMessage
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string AggregateType { get; set; } = null!;

    public int? AggregateId { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = null!;

    [Required]
    public string Payload { get; set; } = null!; // JSON payload

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}