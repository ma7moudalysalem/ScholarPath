using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IBlobStorageService storage,
    ILogger<DownloadDocumentQueryHandler> logger)
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
        {
            // A company reviewer may download documents that are attached to an
            // application submitted against one of their own scholarships.
            var isCompanyReviewer = currentUser.IsInRole("Company")
                && document.ApplicationTrackerId.HasValue
                && await db.Applications
                    .AsNoTracking()
                    .Include(a => a.Scholarship)
                    .AnyAsync(
                        a => a.Id == document.ApplicationTrackerId.Value
                          && a.Scholarship != null
                          && a.Scholarship.OwnerCompanyId == userId,
                        ct)
                    .ConfigureAwait(false);

            if (!isCompanyReviewer)
                throw new ForbiddenAccessException("You can only download your own documents.");
        }

        // Demo / seeded documents store a placeholder StoragePath that doesn't
        // exist in the real blob container. Return a friendly text placeholder
        // instead of bubbling a storage 404 up as a 500.
        if (string.IsNullOrEmpty(document.StoragePath)
            || document.StoragePath.StartsWith("https://demo.blob/", StringComparison.OrdinalIgnoreCase)
            || document.StoragePath.StartsWith("demo:", StringComparison.OrdinalIgnoreCase))
        {
            var placeholder = $"[ScholarPath demo document]\r\n\r\n"
                + $"File:     {document.FileName}\r\n"
                + $"Category: {document.Category}\r\n"
                + $"Uploaded: {document.CreatedAt:yyyy-MM-dd HH:mm} UTC\r\n\r\n"
                + "This is a placeholder for the seeded demo dataset. "
                + "Uploading a real file from the Documents page replaces it with the actual bytes.";
            return new DocumentDownloadDto(
                new MemoryStream(Encoding.UTF8.GetBytes(placeholder)),
                Path.ChangeExtension(document.FileName, ".txt"),
                "text/plain; charset=utf-8");
        }

        try
        {
            var content = await storage.DownloadAsync(document.StoragePath, ct).ConfigureAwait(false);
            return new DocumentDownloadDto(content, document.FileName, document.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document {Id} storage fetch failed at {Path}",
                document.Id, document.StoragePath);
            throw new NotFoundException(nameof(Document), request.DocumentId);
        }
    }
}
