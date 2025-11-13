using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class PaymentMethodUpsertRequest
{
    [Required]
    public string UserId { get; set; }

    [Required, MaxLength(80)]
    public string StripeCustomerId { get; set; } = "";

    [MaxLength(20)]
    public string? Brand { get; set; }

    [StringLength(4)]
    public string? Last4 { get; set; }

    public short? ExpMonth { get; set; }
    public short? ExpYear { get; set; }
    public bool IsDefault { get; set; } = false;
}