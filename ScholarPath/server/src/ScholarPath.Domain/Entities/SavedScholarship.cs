using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class SavedScholarship : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ScholarshipId { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Scholarship Scholarship { get; set; } = null!;
}
