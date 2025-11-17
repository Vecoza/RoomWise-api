namespace RoomWise.Model.Responses;

public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Phone { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public int LoyaltyBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}