using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviews.Queries.GetMyReceivedReviews;

public sealed class GetMyReceivedReviewsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyReceivedReviewsQuery, ReceivedReviewsSummaryDto>
{
    public async Task<ReceivedReviewsSummaryDto> Handle(
        GetMyReceivedReviewsQuery request, CancellationToken ct)
    {
        var companyId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Visible reviews only: an admin-hidden or soft-deleted row must never
        // surface on the company's own page nor count toward the average.
        var baseQuery = db.ScholarshipProviderReviews
            .AsNoTracking()
            .Where(r => r.ScholarshipProviderId == companyId
                        && !r.IsHiddenByAdmin
                        && !r.IsDeleted);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        double averageRating = 0;
        if (total > 0)
        {
            averageRating = await baseQuery
                .AverageAsync(r => (double)r.Rating, ct)
                .ConfigureAwait(false);
        }

        // Project the raw author name parts, then mask in memory — the masking
        // helper is not translatable to SQL.
        var rows = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                FirstName = r.Student != null ? r.Student.FirstName : null,
                LastName = r.Student != null ? r.Student.LastName : null,
                r.CreatedAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reviews = rows
            .Select(r => new ReceivedReviewDto(
                r.Id,
                r.Rating,
                r.Comment,
                ReviewerNameMask.Mask(r.FirstName, r.LastName),
                r.CreatedAt))
            .ToList();

        return new ReceivedReviewsSummaryDto(
            Math.Round(averageRating, 1),
            total,
            reviews);
    }
}
