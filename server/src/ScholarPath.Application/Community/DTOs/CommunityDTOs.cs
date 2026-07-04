namespace ScholarPath.Application.Community.DTOs;

public record ForumPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    Guid? CategoryId,
    // Localized single-language view (English side / legacy), kept for backward
    // compatibility. Prefer the bilingual pair below for display.
    string? Title,
    string BodyMarkdown,
    int UpvoteCount,
    int DownvoteCount,
    int ReplyCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Tags,
    bool IsBookmarked,
    // Bilingual content — the client picks by language with a cross-language
    // fallback (posts are bilingual like scholarships). Replies carry only the
    // single body in TitleEn=null / BodyEn=<body>.
    string? TitleEn = null,
    string? TitleAr = null,
    string BodyEn = "",
    string? BodyAr = null);

public record ForumCategoryDto(
    Guid Id,
    string NameEn,
    string NameAr,
    string Slug,
    string? DescriptionEn,
    string? DescriptionAr,
    int DisplayOrder);

public record ForumThreadDto(
    ForumPostDto Post,
    List<ForumPostDto> Replies);
