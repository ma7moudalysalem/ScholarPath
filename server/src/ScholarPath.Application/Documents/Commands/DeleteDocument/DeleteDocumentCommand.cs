using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Documents.Commands.DeleteDocument;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Deletes a vault document (FR-216). The DB row is soft-deleted and the bytes
/// are removed from storage. Only the owner — or an admin — may delete.
/// </summary>
[Auditable(AuditAction.Delete, "Document",
    TargetIdProperty = nameof(DocumentId),
    SummaryTemplate = "Deleted document {TargetId}")]
public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class DeleteDocumentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService storage,
    IDateTimeService clock,
    ILogger<DeleteDocumentCommandHandler> logger)
    : IRequestHandler<DeleteDocumentCommand>
{
    public async Task Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var isAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");
        if (document.OwnerUserId != userId && !isAdmin)
            throw new ForbiddenAccessException("You can only delete your own documents.");

        var now = clock.UtcNow;
        document.IsDeleted = true;
        document.DeletedAt = now;
        document.DeletedByUserId = userId;
        document.UpdatedAt = now;
        document.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Best-effort byte cleanup — the row is already gone for the user, and a
        // failed storage delete must not surface as an error.
        try
        {
            await storage.DeleteAsync(document.StoragePath, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Document {DocumentId} row deleted but storage cleanup failed.", document.Id);
        }

        logger.LogInformation("Document {DocumentId} deleted by {UserId}.", document.Id, userId);
    }
}
