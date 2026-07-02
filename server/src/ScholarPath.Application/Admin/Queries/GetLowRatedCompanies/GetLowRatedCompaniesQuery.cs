using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetLowRatedCompanies;

/// <summary>
/// Admin queue: companies whose average rating fell below the low-rating
/// threshold and have not yet been triaged (PB-005R). One row per still-flagged
/// company, newest flag first. Resolved into <see cref="LowRatedScholarshipProviderRow"/>s
/// the admin page can render without further joins.
/// </summary>
public sealed record GetLowRatedCompaniesQuery(
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<LowRatedScholarshipProviderRow>>;

public sealed record LowRatedScholarshipProviderRow(
    Guid ScholarshipProviderId,
    string Email,
    string ScholarshipProviderName,
    string? OrganizationLegalName,
    AccountStatus AccountStatus,
    decimal? AverageRating,
    int ReviewCount,
    DateTimeOffset FlaggedAt,
    DateTimeOffset? LastReviewAt);

public sealed class GetLowRatedCompaniesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetLowRatedCompaniesQuery, PagedResult<LowRatedScholarshipProviderRow>>
{
    public async Task<PagedResult<LowRatedScholarshipProviderRow>> Handle(
        GetLowRatedCompaniesQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException();
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Join Users → UserProfiles. The filtered index on
        // UserProfile.ScholarshipProviderLowRatingFlaggedAt keeps the scan small even on a
        // large user base.
        var flaggedQuery =
            from u in db.Users.AsNoTracking()
            join p in db.UserProfiles.AsNoTracking() on u.Id equals p.UserId
            where p.ScholarshipProviderLowRatingFlaggedAt != null
            select new { User = u, Profile = p };

        var total = await flaggedQuery.CountAsync(ct).ConfigureAwait(false);

        var rows = await flaggedQuery
            .OrderByDescending(x => x.Profile.ScholarshipProviderLowRatingFlaggedAt)
            .ThenBy(x => x.User.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LowRatedScholarshipProviderRow(
                x.User.Id,
                x.User.Email ?? string.Empty,
                (x.User.FirstName + " " + x.User.LastName).Trim(),
                x.Profile.OrganizationLegalName,
                x.User.AccountStatus,
                x.Profile.ScholarshipProviderAverageRating,
                x.Profile.ScholarshipProviderReviewCount,
                x.Profile.ScholarshipProviderLowRatingFlaggedAt!.Value,
                db.ScholarshipProviderReviews
                    .Where(r => r.ScholarshipProviderId == x.User.Id && !r.IsHiddenByAdmin)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => (DateTimeOffset?)r.CreatedAt)
                    .FirstOrDefault()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<LowRatedScholarshipProviderRow>(rows, page, pageSize, total);
    }
}
