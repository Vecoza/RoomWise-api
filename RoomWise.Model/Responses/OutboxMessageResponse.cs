namespace RoomWise.Model.Responses;

public class OutboxMessageResponse
{
    public int Id { get; set; }
    public string AggregateType { get; set; } = "";
    public int? AggregateId { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}