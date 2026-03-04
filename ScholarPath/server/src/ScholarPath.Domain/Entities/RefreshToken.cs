using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public Guid UserId { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
