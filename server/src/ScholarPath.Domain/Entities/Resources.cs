using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Resource : AuditableEntity, ISoftDeletable
{
    public string TitleEn { get; set; } = default!;
    public string TitleAr { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? ContentMarkdownEn { get; set; }
    public string? ContentMarkdownAr { get; set; }
    public string? ExternalLinkUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public Guid AuthorUserId { get; set; }
    public string AuthorRole { get; set; } = default!; // Admin, Company, Consultant
    public ResourceType Type { get; set; } = ResourceType.Article;
    public ResourceStatus Status { get; set; } = ResourceStatus.Draft;

    public string? CategorySlug { get; set; }
    public string? TagsJson { get; set; }

    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Author { get; set; }
    public ICollection<ResourceChild> Chapters { get; } = [];
    public ICollection<ResourceBookmark> Bookmarks { get; } = [];
    public ICollection<ResourceProgress> ProgressRecords { get; } = [];
}

public class ResourceChild : BaseEntity
{
    public Guid ResourceId { get; set; }
    public string TitleEn { get; set; } = default!;
    public string TitleAr { get; set; } = default!;
    public string? ContentMarkdownEn { get; set; }
    public string? ContentMarkdownAr { get; set; }
    public int SortOrder { get; set; }
    public int EstimatedReadMinutes { get; set; }
}

public class ResourceBookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public DateTimeOffset BookmarkedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ResourceProgress : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public int ChaptersCompletedCount { get; set; }
    public DateTimeOffset LastAccessedAt { get; set; }

    public ICollection<ResourceProgressChild> ChapterProgress { get; } = [];
}

public class ResourceProgressChild : BaseEntity
{
    public Guid ResourceProgressId { get; set; }
    public Guid ResourceChildId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
