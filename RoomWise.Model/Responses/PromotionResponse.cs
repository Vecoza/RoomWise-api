namespace RoomWise.Model.Responses;

public class PromotionResponse
{
    public int Id { get; set; }
    public int? HotelId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountFixed { get; set; }
    public int MinNights { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}