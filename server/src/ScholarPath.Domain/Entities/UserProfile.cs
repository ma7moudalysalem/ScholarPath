using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public string? FieldOfStudy { get; set; }
    public decimal? GPA { get; set; }
    public string? Interests { get; set; } // JSON array of tags
    public string? Country { get; set; }
    public string? TargetCountry { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
