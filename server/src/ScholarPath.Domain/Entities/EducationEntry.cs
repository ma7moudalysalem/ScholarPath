using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class EducationEntry : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string DegreeName { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public int? EndYear { get; set; }
    public bool IsCurrentlyStudying { get; set; }

    // Navigation
    public UpgradeRequest UpgradeRequest { get; set; } = null!;
}
