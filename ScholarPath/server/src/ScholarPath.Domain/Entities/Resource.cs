using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Resource : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
