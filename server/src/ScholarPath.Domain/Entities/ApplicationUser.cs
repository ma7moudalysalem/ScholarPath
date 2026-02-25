using Microsoft.AspNetCore.Identity;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Unassigned;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
    public bool IsOnboardingComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public UserProfile? UserProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<UpgradeRequest> UpgradeRequests { get; set; } = [];
    public ICollection<SavedScholarship> SavedScholarships { get; set; } = [];
}
