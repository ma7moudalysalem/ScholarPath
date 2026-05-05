using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Resource : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Excerpt { get; set; }
    public string? ExcerptAr { get; set; }
    public ResourceTopic Topic { get; set; }
    public string? ContentHtml { get; set; }
    public string? ContentHtmlAr { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public string? DifficultyLevel { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.Published;
    public int ViewCount { get; set; }

    public ICollection<ResourceAttachment> Attachments { get; set; } = [];

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public ICollection<ResourceBookmark> Bookmarks { get; set; } = [];
}
