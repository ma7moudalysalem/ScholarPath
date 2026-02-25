using Microsoft.AspNetCore.Identity;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
    public bool IsOnboardingComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public UserProfile? UserProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UpgradeRequest> UpgradeRequests { get; set; } = new List<UpgradeRequest>();
    public ICollection<SavedScholarship> SavedScholarships { get; set; } = new List<SavedScholarship>();
}
