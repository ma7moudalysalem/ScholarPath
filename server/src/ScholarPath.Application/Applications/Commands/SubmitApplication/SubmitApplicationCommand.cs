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

            if (!string.IsNullOrWhiteSpace(scholarship.RequiredDocumentsJson)
                && string.IsNullOrWhiteSpace(application.AttachedDocumentsJson))
            {
                throw new ConflictException("Attach the required documents before submitting.");
            }
        }

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
