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
/// the provider's own website. Unlike <c>StartApplicationCommand</c>, no in-app
/// submission happens here — the application is tracked manually, starting in
/// the <see cref="ApplicationStatus.Intending"/> self-tracked state.
/// </summary>
[Auditable(AuditAction.Create, "Application",
    SummaryTemplate = "Registered external application for scholarship {ScholarshipId}")]
public sealed record ExternalIntentCommand(
    Guid ScholarshipId,
    string? ExternalTrackingUrl,
    string? ExternalReferenceId,
    string? PersonalNotes) : IRequest<Guid>;

public sealed class ExternalIntentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ExternalIntentCommand, Guid>
{
    public async Task<Guid> Handle(ExternalIntentCommand request, CancellationToken ct)
    {
        // 1. Verify caller identity
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        // 2. Load scholarship and verify it exists
        var scholarship = await db.Scholarships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        // 3. Only external listings can be tracked this way. An in-app listing
        //    must be applied to via StartApplicationCommand.
        if (scholarship.Mode != ListingMode.ExternalUrl)
            throw new ConflictException(
                "This scholarship is an in-app listing — apply to it directly instead of tracking it externally.");

        // 4. Prevent duplicate active applications.
        //    IsActive is a computed (unmapped) property — inline the terminal-state
        //    check so the predicate translates to SQL.
        var hasActive = await db.Applications.AnyAsync(a =>
            a.StudentId == userId &&
            a.ScholarshipId == request.ScholarshipId &&
            a.Status != ApplicationStatus.Withdrawn &&
            a.Status != ApplicationStatus.Rejected &&
            a.Status != ApplicationStatus.Accepted, ct);

        if (hasActive)
            throw new ConflictException(
                "You already have an active application for this scholarship.");

        // 5. Create the external-tracking entity
        var entity = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            ScholarshipId = request.ScholarshipId,
            Mode = ApplicationMode.External,
            Status = ApplicationStatus.Intending,
            ExternalTrackingUrl = request.ExternalTrackingUrl,
            ExternalReferenceId = request.ExternalReferenceId,
            PersonalNotes = request.PersonalNotes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Applications.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.Id;
    }
}
