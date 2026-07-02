using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;

namespace ScholarPath.Application.ScholarshipProviderReviews.Queries.GetScholarshipProviderRatings;

public sealed class GetScholarshipProviderRatingsQueryHandler(
    IApplicationDbContext db)
    : IRequestHandler<GetScholarshipProviderRatingsQuery, ScholarshipProviderRatingsSummaryDto>
{
    public async Task<ScholarshipProviderRatingsSummaryDto> Handle(GetScholarshipProviderRatingsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = db.ScholarshipProviderReviews
            .AsNoTracking()
            .Where(r => r.ScholarshipProviderId == request.ScholarshipProviderId && !r.IsHiddenByAdmin);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        
        double averageRating = 0;
        if (total > 0)
        {
            averageRating = await baseQuery.AverageAsync(r => r.Rating, ct).ConfigureAwait(false);
        }

        var reviews = await baseQuery
            .Include(r => r.Student)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ScholarshipProviderReviewRow(
                r.Id,
                r.StudentId,
                r.Student != null ? r.Student.FullName : "Unknown",
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ScholarshipProviderRatingsSummaryDto(
            request.ScholarshipProviderId,
            Math.Round(averageRating, 1),
            total,
            reviews);
    }
}
