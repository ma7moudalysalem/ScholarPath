using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Applications.Commands.StartApplication;

/// <summary>
/// Creates a new Draft application for the current student on the given scholarship.
/// </summary>
[Auditable(AuditAction.Create, "Application",
    SummaryTemplate = "Started draft application for scholarship {ScholarshipId}")]
public sealed record StartApplicationCommand(
    Guid ScholarshipId,
    string? PersonalNotes) : IRequest<Guid>;

public sealed class StartApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<StartApplicationCommand, Guid>
{
    public async Task<Guid> Handle(StartApplicationCommand request, CancellationToken ct)
    {
        // 1. Verify caller identity
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        // 2. Load scholarship and verify it exists
        var scholarship = await db.Scholarships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        // 3. B3: block manual applications on external listings
        if (scholarship.Mode == ListingMode.ExternalUrl)
            throw new ConflictException(
                "External listings must be tracked via ExternalIntentCommand.");

        // 4. Scholarship must be open
        if (scholarship.Status != ScholarshipStatus.Open)
            throw new ConflictException(
                "This scholarship is currently closed for applications.");

        // 5. B1: prevent duplicate active applications.
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

        // 6. Create the draft entity
        var entity = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            ScholarshipId = request.ScholarshipId,
            Mode = ApplicationMode.InApp,
            Status = ApplicationStatus.Draft,
            PersonalNotes = request.PersonalNotes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Applications.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.Id;
    }
}
