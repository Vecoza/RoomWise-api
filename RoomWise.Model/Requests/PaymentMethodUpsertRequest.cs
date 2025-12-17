using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoomWise.Model.Requests;

public class PaymentMethodUpsertRequest
{

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string StripePaymentMethodId { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Brand { get; set; }

    [StringLength(4)]
    public string? Last4 { get; set; }

    public short? ExpMonth { get; set; }
    public short? ExpYear { get; set; }
    public bool IsDefault { get; set; } = false;
}
