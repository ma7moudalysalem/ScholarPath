using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviews.DTOs;

namespace ScholarPath.Application.CompanyReviews.Queries.GetCompanyRatings;

public sealed class GetCompanyRatingsQueryHandler(
    IApplicationDbContext db)
    : IRequestHandler<GetCompanyRatingsQuery, CompanyRatingsSummaryDto>
{
    public async Task<CompanyRatingsSummaryDto> Handle(GetCompanyRatingsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = db.CompanyReviews
            .AsNoTracking()
            .Where(r => r.CompanyId == request.CompanyId && !r.IsHiddenByAdmin);

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
            .Select(r => new CompanyReviewRow(
                r.Id,
                r.StudentId,
                r.Student != null ? r.Student.FullName : "Unknown",
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new CompanyRatingsSummaryDto(
            request.CompanyId,
            Math.Round(averageRating, 1),
            total,
            reviews);
    }
}
