using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Resources;

public sealed record ResourceChapterDto(
    Guid Id,
    string TitleEn,
    string TitleAr,
    string? ContentMarkdownEn,
    string? ContentMarkdownAr,
    int SortOrder,
    int EstimatedReadMinutes);

public sealed record ResourceListItemDto(
    Guid Id,
    string Slug,
    string TitleEn,
    string TitleAr,
    string? DescriptionEn,
    string? DescriptionAr,
    ResourceType Type,
    ResourceStatus Status,
    string? CategorySlug,
    string? CoverImageUrl,
    string AuthorRole,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    int FeaturedOrder,
    DateTimeOffset? PublishedAt);

public sealed record ResourceDetailDto(
    Guid Id,
    string Slug,
    string TitleEn,
    string TitleAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string? ContentMarkdownEn,
    string? ContentMarkdownAr,
    string? ExternalLinkUrl,
    string? CoverImageUrl,
    Guid AuthorUserId,
    string AuthorRole,
    string? AuthorName,
    ResourceType Type,
    ResourceStatus Status,
    string? CategorySlug,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    DateTimeOffset? PublishedAt,
    string? RejectionReason,
    IReadOnlyList<ResourceChapterDto> Chapters);

public sealed record ResourceBookmarkDto(
    Guid ResourceId,
    string Slug,
    string TitleEn,
    string TitleAr,
    ResourceType Type,
    string? CoverImageUrl,
    DateTimeOffset BookmarkedAt);

public sealed record ResourceProgressDto(
    Guid ResourceId,
    string Slug,
    string TitleEn,
    string TitleAr,
    int ChaptersCompletedCount,
    int TotalChapters,
    DateTimeOffset LastAccessedAt);

/// <summary>Chapter payload accepted by Create/Update resource commands.</summary>
public sealed record ResourceChapterInput(
    string TitleEn,
    string TitleAr,
    string? ContentMarkdownEn,
    string? ContentMarkdownAr,
    int SortOrder,
    int EstimatedReadMinutes);
