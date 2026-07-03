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
    string? PersonalNotes) : IRequest<StartApplicationResult>;

/// <summary>
/// Outcome of <see cref="StartApplicationCommand"/>. <see cref="AlreadyExisted"/>
/// is <see langword="true"/> when the student already had a non-terminal
/// application for the scholarship, so it was resumed rather than created.
/// </summary>
public sealed record StartApplicationResult(Guid ApplicationId, bool AlreadyExisted);

public sealed class StartApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<StartApplicationCommand, StartApplicationResult>
{
    public async Task<StartApplicationResult> Handle(StartApplicationCommand request, CancellationToken ct)
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

        // TC-003: the student must complete the core of their profile before
        // applying — without it the application is unreviewable and AI
        // personalisation has no data to work with. Surfaced as a clear,
        // actionable error rather than a misleading "scholarship closed".
        var profile = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Profile)
            .FirstOrDefaultAsync(ct);

        if (profile is null
            || profile.AcademicLevel is null
            || string.IsNullOrWhiteSpace(profile.FieldOfStudy))
        {
            throw new ConflictException(
                "Complete your profile — add your academic level and field of study — before applying for scholarships.");
        }

        // 4. Scholarship must be open
        if (scholarship.Status != ScholarshipStatus.Open)
            throw new ConflictException(
                "This scholarship is currently closed for applications.");

        // 4b. FR-043/044: the application deadline must not have passed. The
        //     auto-close job (ScholarshipAutoCloseJob) flips Status→Closed only on
        //     a schedule, so there is a window where Status is still Open but the
        //     deadline has already lapsed — enforce it here as defense-in-depth.
        //     Compared in UTC (DateTimeOffset comparison is instant-based).
        if (scholarship.Deadline < DateTimeOffset.UtcNow)
            throw new ConflictException(
                "This scholarship's application deadline has passed.");

        // 5. B1: an in-app application is idempotent per (student, scholarship).
        //    If the student already has a non-terminal application, resume it
        //    instead of dead-ending on a 409 — a repeated "Apply" click then
        //    simply reopens the existing draft. IsActive is a computed (unmapped)
        //    property, so the terminal-state check is inlined to translate to SQL.
        var existingId = await db.Applications
            .Where(a =>
                a.StudentId == userId &&
                a.ScholarshipId == request.ScholarshipId &&
                a.Status != ApplicationStatus.Withdrawn &&
                a.Status != ApplicationStatus.Rejected &&
                a.Status != ApplicationStatus.Accepted)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (existingId is not null)
            return new StartApplicationResult(existingId.Value, AlreadyExisted: true);

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

        return new StartApplicationResult(entity.Id, AlreadyExisted: false);
    }
}
