namespace ScholarPath.Application.CompanyReviews.DTOs;

public record CompanyReviewRow(
    Guid ReviewId,
    Guid StudentId,
    string StudentName,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

public record CompanyRatingsSummaryDto(
    Guid CompanyId,
    double AverageRating,
    int TotalRatings,
    IReadOnlyList<CompanyReviewRow> RecentReviews);

// ─── "Reviews received" read model (shared Company + Consultant) ───────────────

/// <summary>
/// One review row as shown to the rated party (Company or Consultant) on their
/// own "Reviews received" page. The author name is masked
/// (<see cref="Common.ReviewerNameMask"/>) so feedback stays semi-anonymous.
/// </summary>
public record ReceivedReviewDto(
    Guid Id,
    int Rating,
    string? Comment,
    string AuthorName,
    DateTimeOffset CreatedAt);

/// <summary>
/// The authenticated user's received-reviews summary: an aggregate average +
/// count over visible reviews, plus the newest-first review list. Backs
/// <c>GET /api/company-reviews/mine</c> and <c>GET /api/consultant/reviews/mine</c>.
/// </summary>
public record ReceivedReviewsSummaryDto(
    double AverageRating,
    int TotalReviews,
    IReadOnlyList<ReceivedReviewDto> Reviews);
