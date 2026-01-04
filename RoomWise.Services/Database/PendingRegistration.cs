using System.ComponentModel.DataAnnotations;

namespace RoomWise.Model;

public sealed class PendingRegistration
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(512)]
    public string PasswordHash { get; set; } = null!;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string CodeHash { get; set; } = null!;

    public DateTime CodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSentAt { get; set; }

    public int AttemptCount { get; set; } = 0;
}
