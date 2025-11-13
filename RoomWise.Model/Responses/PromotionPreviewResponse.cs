namespace RoomWise.Model.Responses;

public class PromotionPreviewResponse
{
    public int? PromotionId { get; set; }
    public string? Title { get; set; }
    public decimal DiscountedNightly { get; set; }
    public decimal TotalBefore { get; set; }
    public decimal TotalAfter { get; set; }
}