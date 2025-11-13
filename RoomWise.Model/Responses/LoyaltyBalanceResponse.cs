namespace RoomWise.Model.Responses;

public class LoyaltyBalanceResponse
{
    public string UserId { get; set; } = null!;
    public int Balance { get; set; }
}