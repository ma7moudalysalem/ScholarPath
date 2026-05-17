using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Documents.Queries.DownloadDocument;

/// <summary>
/// Streams a vault document's bytes (FR-216). Only the owner — or an admin —
/// may download a document.
/// </summary>
public sealed record DownloadDocumentQuery(Guid DocumentId) : IRequest<DocumentDownloadDto>;

public sealed class DownloadDocumentQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService storage)
    : IRequestHandler<DownloadDocumentQuery, DocumentDownloadDto>
{
    public async Task<DocumentDownloadDto> Handle(DownloadDocumentQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var document = await db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var isAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");
        if (document.OwnerUserId != userId && !isAdmin)
            throw new ForbiddenAccessException("You can only download your own documents.");

        var content = await storage.DownloadAsync(document.StoragePath, ct).ConfigureAwait(false);
        return new DocumentDownloadDto(content, document.FileName, document.ContentType);
    }
}
