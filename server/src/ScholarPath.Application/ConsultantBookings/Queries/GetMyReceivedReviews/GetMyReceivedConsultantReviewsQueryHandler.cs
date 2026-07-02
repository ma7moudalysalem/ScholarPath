using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetMyReceivedReviews;

public sealed class GetMyReceivedConsultantReviewsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyReceivedConsultantReviewsQuery, ReceivedReviewsSummaryDto>
{
    public async Task<ReceivedReviewsSummaryDto> Handle(
        GetMyReceivedConsultantReviewsQuery request, CancellationToken ct)
    {
        var consultantId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Visible reviews only — admin-hidden or soft-deleted rows are excluded
        // from both the list and the average (mirrors the public detail page).
        var baseQuery = db.ConsultantReviews
            .AsNoTracking()
            .Where(r => r.ConsultantId == consultantId
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
