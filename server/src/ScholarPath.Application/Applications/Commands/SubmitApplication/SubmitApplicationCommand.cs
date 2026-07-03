using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.Common;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Applications.Commands.SubmitApplication;

/// <summary>
/// Transitions a Draft application to Pending, marking it as formally submitted.
/// </summary>
[Auditable(AuditAction.Update, "Application",
    SummaryTemplate = "Submitted application {ApplicationId}")]
public sealed record SubmitApplicationCommand(Guid ApplicationId) : IRequest;

public sealed class SubmitApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<SubmitApplicationCommand>
{
    public async Task Handle(SubmitApplicationCommand request, CancellationToken ct)
    {
        // 1. Verify caller identity
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        // 2. Load application and verify ownership
        var application = await db.Applications
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a =>
                a.Id == request.ApplicationId &&
                a.StudentId == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationTracker), request.ApplicationId);

        // 3. Validate state transition via state machine
        var oldStatus = application.Status;
        ApplicationStateMachine.EnsureTransition(oldStatus, ApplicationStatus.Pending);

        // 3b. Completeness guard — an in-app application must actually be filled
        // in (form answers + required documents) before it can leave Draft.
        if (application.Mode == ApplicationMode.InApp && application.Scholarship is { } scholarship)
        {
            if (!string.IsNullOrWhiteSpace(scholarship.ApplicationFormSchemaJson)
                && string.IsNullOrWhiteSpace(application.FormDataJson))
            {
                throw new ConflictException("Complete the application form before submitting.");
            }

            if (!string.IsNullOrWhiteSpace(scholarship.RequiredDocumentsJson))
            {
                var required = System.Text.Json.JsonSerializer
                    .Deserialize<string[]>(scholarship.RequiredDocumentsJson) ?? [];
                if (required.Length > 0)
                {
                    var attached = string.IsNullOrWhiteSpace(application.AttachedDocumentsJson)
                        ? []
                        : System.Text.Json.JsonSerializer
                            .Deserialize<string[]>(application.AttachedDocumentsJson) ?? [];

                    // Parallel arrays: required[i] maps to attached[i].
                    // Every slot must be filled (non-empty URL).
                    var missing = required
                        .Select((name, i) => new { name, url = attached.ElementAtOrDefault(i) })
                        .Where(x => string.IsNullOrWhiteSpace(x.url))
                        .Select(x => x.name)
                        .ToList();

                    if (missing.Count > 0)
                        throw new ConflictException(
                            $"Missing required documents: {string.Join(", ", missing)}. Upload all required files before submitting.");
                }
            }
        }

        // 3c. FR-043/044: reject submission after the deadline has passed.
        //     Enforced here (not only at Start) because a Draft may sit past the
        //     deadline before being submitted. Guarded against a null-included
        //     Scholarship; compared in UTC.
        if (application.Scholarship is { } sch && sch.Deadline < DateTimeOffset.UtcNow)
            throw new ConflictException(
                "This scholarship's application deadline has passed.");

        // 4. Apply transition
        application.Status = ApplicationStatus.Pending;
        application.SubmittedAt = DateTimeOffset.UtcNow;

        // 5. Raise domain events
        application.RaiseDomainEvent(new ApplicationSubmittedEvent(
            application.Id,
            application.StudentId,
            application.ScholarshipId));

        application.RaiseDomainEvent(new ApplicationStatusChangedEvent(
            application.Id,
            application.StudentId,
            application.ScholarshipId,
            OldStatus: oldStatus,
            NewStatus: ApplicationStatus.Pending));

        await db.SaveChangesAsync(ct);
    }
}
