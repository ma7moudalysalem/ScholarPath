using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Applications.Commands.SaveApplicationDraft;

/// <summary>
/// Persists the in-progress answers, attached documents and personal notes on a
/// Draft application owned by the current student. Only a Draft can be edited —
/// once submitted an application is locked.
/// </summary>
[Auditable(AuditAction.Update, "Application",
    SummaryTemplate = "Saved application draft {ApplicationId}")]
public sealed record SaveApplicationDraftCommand(
    Guid ApplicationId,
    string? FormDataJson,
    string? AttachedDocumentsJson,
    string? PersonalNotes) : IRequest;

public sealed class SaveApplicationDraftCommandValidator
    : AbstractValidator<SaveApplicationDraftCommand>
{
    public SaveApplicationDraftCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.FormDataJson).MaximumLength(16_000);
        RuleFor(x => x.AttachedDocumentsJson).MaximumLength(8_000);
        RuleFor(x => x.PersonalNotes).MaximumLength(4_000);
    }
}

public sealed class SaveApplicationDraftCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<SaveApplicationDraftCommand>
{
    public async Task Handle(SaveApplicationDraftCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        var application = await db.Applications
            .FirstOrDefaultAsync(
                a => a.Id == request.ApplicationId && a.StudentId == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationTracker), request.ApplicationId);

        // A submitted application is read-only — only a Draft can still be edited.
        if (application.Status != ApplicationStatus.Draft)
        {
            throw new ConflictException("Only a draft application can be edited.");
        }

        application.FormDataJson = request.FormDataJson;
        application.AttachedDocumentsJson = request.AttachedDocumentsJson;
        application.PersonalNotes = request.PersonalNotes;
        application.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
