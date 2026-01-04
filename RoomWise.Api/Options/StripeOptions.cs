namespace RoomWise.Api.Options;

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool DisableWebhookSignature { get; set; } = false;
}
