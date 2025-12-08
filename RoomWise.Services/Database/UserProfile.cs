using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class UserProfile
{
    [Key] [ForeignKey(nameof(User))] public string UserId { get; set; } = string.Empty;                // FK -> AspNetUsers.Id

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = null!;

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    public int LoyaltyBalance { get; set; } = 0;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation to Identity user
    public virtual AppUser? User { get; set; }
}
