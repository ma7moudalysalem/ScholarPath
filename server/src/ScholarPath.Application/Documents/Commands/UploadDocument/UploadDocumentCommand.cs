using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Documents.Commands.UploadDocument;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Uploads a file to the caller's personal document vault (FR-216). The bytes
/// are written to the configured storage provider; only metadata is persisted.
/// </summary>
[Auditable(AuditAction.Create, "Document", SummaryTemplate = "Uploaded document {FileName}")]
public sealed record UploadDocumentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    DocumentCategory Category,
    Guid? ApplicationTrackerId) : IRequest<DocumentDto>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(UploadDocumentCommandHandler.MaxBytes)
            .WithMessage("A document must be 25 MB or smaller.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class UploadDocumentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService storage,
    IDateTimeService clock,
    ILogger<UploadDocumentCommandHandler> logger)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public const long MaxBytes = 25 * 1024 * 1024;
    private const string Container = "documents";

    // Extensions a document vault is expected to hold — keeps executables out.
    private static readonly string[] AllowedExtensions =
        [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp", ".txt", ".rtf", ".odt"];

    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException(
                "Unsupported file type. Allowed: PDF, Word, image, or text documents.");

        // A document may only be linked to one of the caller's own applications.
        if (request.ApplicationTrackerId is { } appId)
        {
            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == appId && a.StudentId == userId, ct)
                .ConfigureAwait(false);
            if (!ownsApplication)
                throw new NotFoundException(nameof(ApplicationTracker), appId);
        }

        var safeName = Path.GetFileName(request.FileName);
        var storagePath = await storage
            .UploadAsync(request.Content, safeName, request.ContentType, Container, ct)
            .ConfigureAwait(false);

        var now = clock.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            FileName = safeName,
            ContentType = request.ContentType,
            SizeBytes = request.Length,
            StoragePath = storagePath,
            Category = request.Category,
            UploadedAt = now,
            ApplicationTrackerId = request.ApplicationTrackerId,
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Document {DocumentId} uploaded by {UserId} ({Bytes} bytes).",
            document.Id, userId, request.Length);

        return DocumentMapping.ToDto(document);
    }
}
