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

            var attached = string.IsNullOrWhiteSpace(application.AttachedDocumentsJson)
                ? []
                : System.Text.Json.JsonSerializer
                    .Deserialize<string[]>(application.AttachedDocumentsJson) ?? [];

            if (!string.IsNullOrWhiteSpace(scholarship.RequiredDocumentsJson))
            {
                var required = System.Text.Json.JsonSerializer
                    .Deserialize<string[]>(scholarship.RequiredDocumentsJson) ?? [];
                if (required.Length > 0)
                {
                    // Parallel arrays: required[i] maps to attached[i].
                    // Every slot must be filled (non-empty file name).
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

            // FR-APP-13/14: every attached document must reference a file the
            // student actually uploaded through the scanned Document vault (which
            // enforces type/size/antivirus and fail-closes — a Document row only
            // exists once the file passed those checks). Reject any attached entry
            // that doesn't map to a Document owned by this student, so a
            // hand-crafted draft PUT can't submit arbitrary, never-scanned
            // "documents". Vault reuse (any owned file) is allowed by design.
            var attachedNames = attached
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();
            if (attachedNames.Count > 0)
            {
                // Load the backing vault Document ENTITIES (not just names) so we
                // can both validate ownership AND link them to this application.
                var ownedDocs = await db.Documents
                    .Where(d => d.OwnerUserId == userId && !d.IsDeleted && attachedNames.Contains(d.FileName))
                    .ToListAsync(ct);

                var ownedNames = ownedDocs.Select(d => d.FileName).ToHashSet();
                var unbacked = attachedNames.Where(n => !ownedNames.Contains(n)).ToList();
                if (unbacked.Count > 0)
                    throw new ConflictException(
                        $"These attached documents weren't found in your uploaded files: {string.Join(", ", unbacked)}. Upload them through the application form.");

                // Link the backing documents to THIS application so the reviewing
                // provider can actually see and download them. Reuse-from-vault
                // previously copied only the file NAME into AttachedDocumentsJson and
                // never linked the Document row, so the provider's
                // ApplicationTrackerId-keyed query (and the download authz gate) found
                // nothing — the "No documents submitted" bug the provider hit even
                // though the student clearly attached files. `??=` so a file already
                // linked to another application isn't yanked off it.
                foreach (var doc in ownedDocs)
                    doc.ApplicationTrackerId ??= application.Id;
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
