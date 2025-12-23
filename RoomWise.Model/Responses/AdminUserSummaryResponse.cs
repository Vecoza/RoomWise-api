namespace RoomWise.Model.Responses;

public sealed class AdminUserSummaryResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int LoyaltyBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}
