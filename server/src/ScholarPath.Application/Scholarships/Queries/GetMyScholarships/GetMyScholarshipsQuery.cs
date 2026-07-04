using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries.GetMyScholarships;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>A row in the company's own-scholarships list (PB-003 company view).</summary>
public sealed record MyScholarshipDto(
    Guid Id,
    string TitleEn,
    string TitleAr,
    string? Slug,
    ScholarshipStatus Status,
    ListingMode Mode,
    DateTimeOffset Deadline,
    int ApplicantCount,
    DateTimeOffset CreatedAt,
    string? RejectionReason = null);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists every scholarship owned by the authenticated company, newest first.
/// </summary>
public sealed record GetMyScholarshipsQuery : IRequest<IReadOnlyList<MyScholarshipDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyScholarshipsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyScholarshipsQuery, IReadOnlyList<MyScholarshipDto>>
{
    public async Task<IReadOnlyList<MyScholarshipDto>> Handle(GetMyScholarshipsQuery request, CancellationToken ct)
    {
        var companyId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.Scholarships
            .AsNoTracking()
            .Where(s => s.OwnerScholarshipProviderId == companyId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new MyScholarshipDto(
                s.Id,
                s.TitleEn,
                s.TitleAr,
                s.Slug,
                s.Status,
                s.Mode,
                s.Deadline,
                s.Applications.Count(a => !a.IsDeleted),
                s.CreatedAt,
                s.RejectionReason))
            .ToListAsync(ct);
    }
}
