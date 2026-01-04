using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomWise.Model;

public class EmailVerification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(128)]
    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public DateTime? LastSentAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;
}
