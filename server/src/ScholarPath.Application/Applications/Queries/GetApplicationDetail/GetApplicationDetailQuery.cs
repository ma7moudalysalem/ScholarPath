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
    string? ScholarshipProviderName,
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
    DateTimeOffset? DecisionAt,
    // Surfaced so the student UI can decide whether to gate "Submit" behind
    // a ScholarshipProviderReview payment confirmation. Null/0 = no fee required.
    decimal? ReviewFeeUsd,
    // FR-APP-35: the in-app scholarship's owning provider, and whether this
    // student has already rated them for this application. Together they let the
    // student UI show a "Rate provider" action after a final decision and hide
    // it once a rating exists. Null provider = external listing (nothing to rate).
    Guid? ScholarshipProviderId,
    bool HasReview,
    // FR-APP-19: the real recorded status transitions, oldest-first, so the
    // student sees an actual timeline rather than a fixed set of scalar dates.
    IReadOnlyList<ApplicationStatusEntryDto> StatusHistory);

/// <summary>One recorded status transition on the application timeline.</summary>
public sealed record ApplicationStatusEntryDto(string Status, DateTimeOffset OccurredAt);

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
                .ThenInclude(s => s!.OwnerScholarshipProvider)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, ct);

        if (application is null)
            return null;

        // Only the owning student or an administrator may view an application.
        if (application.StudentId != userId && !currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException();

        var scholarship = application.Scholarship;

        // FR-APP-35: one rating per finalized application. Surface whether it
        // already exists so the UI only offers "Rate provider" once.
        var hasReview = await db.ScholarshipProviderReviews
            .AsNoTracking()
            .AnyAsync(r => r.ApplicationTrackerId == application.Id, ct);

        // FR-APP-19: the recorded status-transition rows (written by
        // ApplicationStatusHistoryEventHandler), oldest-first.
        var statusHistory = await db.ApplicationChildren
            .AsNoTracking()
            .Where(c => c.ApplicationTrackerId == application.Id && c.ChildType == "StatusHistory")
            .OrderBy(c => c.OccurredAt)
            .Select(c => new ApplicationStatusEntryDto(c.Title ?? "", c.OccurredAt))
            .ToListAsync(ct);

        return new ApplicationDetailDto(
            application.Id,
            application.ScholarshipId,
            scholarship?.TitleEn ?? application.ExternalTitle ?? "N/A",
            scholarship?.TitleAr ?? application.ExternalTitle ?? "غير محدد",
            scholarship?.OwnerScholarshipProvider?.FullName ?? application.ExternalProvider,
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
            application.DecisionAt,
            scholarship?.ReviewFeeUsd,
            scholarship?.OwnerScholarshipProviderId,
            hasReview,
            statusHistory);
    }
}
