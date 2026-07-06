using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Applications.Commands.ExternalIntent;

/// <summary>
/// Registers an external-listing application the current student is pursuing on
/// the provider's own website. Two flavours:
///   1) <c>ScholarshipId</c> is provided and points to an external-mode ScholarPath
///      listing — same behaviour as before.
///   2) <c>ScholarshipId</c> is null — the scholarship is NOT in the platform
///      catalogue; the tracker is anchored on free-text <c>Title</c> /
///      <c>Provider</c> instead. This is the path the "Add External Application"
///      modal takes for genuinely off-platform scholarships.
/// </summary>
[Auditable(AuditAction.Create, "Application",
    SummaryTemplate = "Registered external application for scholarship {ScholarshipId}")]
public sealed record ExternalIntentCommand(
    Guid? ScholarshipId,
    string? ExternalTrackingUrl,
    string? ExternalReferenceId,
    string? PersonalNotes,
    string? Title = null,
    string? Provider = null,
    DateTimeOffset? Deadline = null) : IRequest<Guid>;

public sealed class ExternalIntentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ExternalIntentCommand, Guid>
{
    public async Task<Guid> Handle(ExternalIntentCommand request, CancellationToken ct)
    {
        // 1. Verify caller identity
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        var entity = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            Mode = ApplicationMode.External,
            Status = ApplicationStatus.Intending,
            ExternalTrackingUrl = request.ExternalTrackingUrl,
            ExternalReferenceId = request.ExternalReferenceId,
            PersonalNotes = request.PersonalNotes,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // 2. Branch on whether the tracker is anchored to a platform scholarship.
        if (request.ScholarshipId is { } scholarshipId && scholarshipId != Guid.Empty)
        {
            // — Linked path: validate the scholarship is an external-mode listing
            //   and the student doesn't already have an active application for it.
            var scholarship = await db.Scholarships
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == scholarshipId, ct)
                ?? throw new NotFoundException(nameof(Scholarship), scholarshipId);

            if (scholarship.Mode != ListingMode.ExternalUrl)
                throw new ConflictException(
                    "This scholarship is an in-app listing — apply to it directly instead of tracking it externally.");

            var hasActive = await db.Applications.AnyAsync(a =>
                a.StudentId == userId &&
                a.ScholarshipId == scholarshipId &&
                a.Status != ApplicationStatus.Withdrawn &&
                a.Status != ApplicationStatus.Rejected &&
                a.Status != ApplicationStatus.Accepted, ct);

            if (hasActive)
                throw new ConflictException(
                    "You already have an active application for this scholarship.");

            entity.ScholarshipId = scholarshipId;
        }
        else
        {
            // — Free-text path: the scholarship lives outside the catalogue, so a
            //   title is mandatory; provider/deadline are nice-to-have.
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ConflictException(
                    "A scholarship title is required when adding an off-platform external application.");

            // A tracked deadline that already lapsed makes no sense for a
            // scholarship the student is still pursuing — reject a past date
            // (day-granular, so "today" is still allowed). Defense-in-depth behind
            // the client's min-date guard.
            if (request.Deadline is { } deadline
                && deadline.UtcDateTime.Date < DateTimeOffset.UtcNow.Date)
            {
                throw new ConflictException("The application deadline can't be in the past.");
            }

            entity.ExternalTitle = request.Title.Trim();
            entity.ExternalProvider = string.IsNullOrWhiteSpace(request.Provider)
                ? null : request.Provider.Trim();
            entity.Deadline = request.Deadline;
        }

        db.Applications.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.Id;
    }
}
