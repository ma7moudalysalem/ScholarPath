using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class SuccessStory : AuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ContentAr { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
