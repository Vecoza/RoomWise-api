using System;
using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model.Requests;

public class UserProfileUpsertRequest
{
    [Required, MaxLength(80)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(80)]
    public string LastName { get; set; } = "";

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "en";
}