using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationNotes;

/// <summary>
/// Updates the student's own personal notes / required-documents free text on a
/// non-terminal application. Unlike <c>SaveApplicationDraftCommand</c> (Draft-only,
/// which persists form answers + attachments for in-app submission), this is a
/// life-of-request notes edit — allowed for an EXTERNAL tracker (or any active
/// application) until a final decision (Accepted/Rejected/Withdrawn) locks it.
/// This is the write path external applications need: they are created as
/// <c>Intending</c>, never <c>Draft</c>, so SaveApplicationDraft rejected every
/// notes edit.
/// </summary>
[Auditable(AuditAction.Update, "Application",
    SummaryTemplate = "Updated notes on application {ApplicationId}")]
public sealed record UpdateApplicationNotesCommand(
    Guid ApplicationId,
    string? PersonalNotes) : IRequest;

public sealed class UpdateApplicationNotesCommandValidator
    : AbstractValidator<UpdateApplicationNotesCommand>
{
    public UpdateApplicationNotesCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.PersonalNotes).MaximumLength(4_000);
    }
}

public sealed class UpdateApplicationNotesCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<UpdateApplicationNotesCommand>
{
    public async Task Handle(UpdateApplicationNotesCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.StudentId == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationTracker), request.ApplicationId);

        // Editable throughout the life of the request — but once a final decision
        // is reached (Accepted / Rejected / Withdrawn) the record is locked.
        if (!application.IsActive)
            throw new ConflictException("This application can no longer be edited.");

        application.PersonalNotes = request.PersonalNotes;
        application.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
