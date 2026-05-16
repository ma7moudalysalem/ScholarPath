using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// One-time password-reset token (PB-001). The raw token is emailed to the
/// user; only its SHA-256 hash is stored. Single-use (UsedAt) and time-boxed.
/// </summary>
public class PasswordResetToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public ApplicationUser? User { get; set; }
}
