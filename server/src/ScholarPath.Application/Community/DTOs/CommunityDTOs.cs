namespace ScholarPath.Application.Community.DTOs;

public record ForumPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    Guid? CategoryId,
    string? Title,
    string BodyMarkdown,
    int UpvoteCount,
    int DownvoteCount,
    int ReplyCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Tags,
    bool IsBookmarked);

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
