using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public Guid UserId { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
