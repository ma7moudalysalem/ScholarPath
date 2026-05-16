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
