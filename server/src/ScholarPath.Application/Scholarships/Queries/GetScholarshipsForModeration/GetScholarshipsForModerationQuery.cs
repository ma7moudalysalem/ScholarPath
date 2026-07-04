using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.Queries.GetMyScholarships;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries.GetScholarshipsForModeration;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Admin moderation list of scholarships filtered by status (defaults to
/// <see cref="ScholarshipStatus.UnderReview"/> — the pending-moderation state),
/// newest first and paged.
/// </summary>
public sealed record GetScholarshipsForModerationQuery(
    ScholarshipStatus Status = ScholarshipStatus.UnderReview,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<MyScholarshipDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetScholarshipsForModerationQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipsForModerationQuery, PaginatedList<MyScholarshipDto>>
{
    public async Task<PaginatedList<MyScholarshipDto>> Handle(
        GetScholarshipsForModerationQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can moderate scholarships.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var projected = db.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == request.Status && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Select(s => new MyScholarshipDto(
                s.Id,
                s.TitleEn,
                s.TitleAr,
                s.Slug,
                s.Status,
                s.Mode,
                s.Deadline,
                s.Applications.Count(a => !a.IsDeleted),
                s.CreatedAt));

        return await PaginatedList<MyScholarshipDto>.CreateAsync(projected, page, pageSize);
    }
}
