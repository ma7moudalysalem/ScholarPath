using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetApplicationDetail;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>Full detail of a single application — student/admin read-side (PB-004).</summary>
public sealed record ApplicationDetailDto(
    Guid Id,
    Guid? ScholarshipId,
    string ScholarshipTitleEn,
    string ScholarshipTitleAr,
    string? CompanyName,
    ApplicationStatus Status,
    ApplicationMode Mode,
    string? FormDataJson,
    string? AttachedDocumentsJson,
    string? ExternalTrackingUrl,
    string? ExternalReferenceId,
    string? DecisionReason,
    string? PersonalNotes,
    DateTimeOffset? Deadline,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ReviewStartedAt,
    DateTimeOffset? DecisionAt);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Loads a single application. The caller must be the owning student or an Admin —
/// otherwise <see cref="ForbiddenAccessException"/> is thrown. Returns <c>null</c>
/// when the application does not exist (controller maps that to a 404).
/// </summary>
public sealed record GetApplicationDetailQuery(Guid ApplicationId) : IRequest<ApplicationDetailDto?>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetApplicationDetailQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetApplicationDetailQuery, ApplicationDetailDto?>
{
    public async Task<ApplicationDetailDto?> Handle(GetApplicationDetailQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var application = await db.Applications
            .AsNoTracking()
            .Include(a => a.Scholarship)
                .ThenInclude(s => s!.OwnerCompany)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, ct);

        if (application is null)
            return null;

        // Only the owning student or an administrator may view an application.
        if (application.StudentId != userId && !currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException();

        var scholarship = application.Scholarship;

        return new ApplicationDetailDto(
            application.Id,
            application.ScholarshipId,
            scholarship?.TitleEn ?? application.ExternalTitle ?? "N/A",
            scholarship?.TitleAr ?? application.ExternalTitle ?? "غير محدد",
            scholarship?.OwnerCompany?.FullName ?? application.ExternalProvider,
            application.Status,
            application.Mode,
            application.FormDataJson,
            application.AttachedDocumentsJson,
            application.ExternalTrackingUrl,
            application.ExternalReferenceId,
            application.DecisionReason,
            application.PersonalNotes,
            scholarship?.Deadline ?? application.Deadline,
            application.CreatedAt,
            application.UpdatedAt,
            application.SubmittedAt,
            application.ReviewStartedAt,
            application.DecisionAt);
    }
}
