using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public sealed class RequestEmailVerificationRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
