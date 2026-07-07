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
    Guid? ApplicationTrackerId,
    // FR-ONB-12 — the specific onboarding document type, when Category is
    // OnboardingDocument. Null for every other upload.
    OnboardingDocumentType? OnboardingType = null,
    // PB-005 — optional link to a paid provider review/support request, so the
    // student can attach files for the provider to review. Null for every other
    // upload.
    Guid? ScholarshipProviderReviewRequestId = null) : IRequest<DocumentDto>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.OnboardingType).IsInEnum().When(x => x.OnboardingType.HasValue);
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
    IFileScanService fileScan,
    IDateTimeService clock,
    ILogger<UploadDocumentCommandHandler> logger)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public const long MaxBytes = 25 * 1024 * 1024;
    private const string Container = "documents";

    // Extensions a document vault is expected to hold — keeps executables out.
    // Each maps to the MIME type(s) the declared ContentType must match, so a
    // spoofed/mismatched client ContentType is rejected up front (DATA-03).
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = ["application/pdf"],
            [".doc"]  = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".jpg"]  = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"]  = ["image/png"],
            [".webp"] = ["image/webp"],
            [".txt"]  = ["text/plain"],
            [".rtf"]  = ["application/rtf", "text/rtf"],
            [".odt"]  = ["application/vnd.oasis.opendocument.text"],
        };

    // Magic-byte signatures per extension — the file's actual leading bytes must
    // match, so a disguised file (e.g. an .exe renamed .pdf with a faked
    // Content-Type) is rejected even when the antivirus provider is a no-op.
    // Text (.txt) has no signature, so it's accepted on the extension+MIME check
    // alone. .webp additionally requires "WEBP" at offset 8 (handled below).
    private static readonly IReadOnlyDictionary<string, byte[][]> Signatures =
        new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = [[0x25, 0x50, 0x44, 0x46]],                          // %PDF
            [".png"]  = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],  // PNG
            [".jpg"]  = [[0xFF, 0xD8, 0xFF]],
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".webp"] = [[0x52, 0x49, 0x46, 0x46]],                          // RIFF (+ WEBP@8)
            [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],                          // ZIP (OOXML)
            [".odt"]  = [[0x50, 0x4B, 0x03, 0x04]],                          // ZIP (ODF)
            [".doc"]  = [[0xD0, 0xCF, 0x11, 0xE0]],                          // OLE2
            [".rtf"]  = [[0x7B, 0x5C, 0x72, 0x74, 0x66]],                    // {\rtf
        };

    public async Task<DocumentDto> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var extension = Path.GetExtension(request.FileName);
        if (!AllowedTypes.TryGetValue(extension, out var allowedContentTypes))
            throw new ConflictException(
                "Unsupported file type. Allowed: PDF, Word, image, or text documents.");

        // DATA-03: the declared MIME type must match the file's extension — a
        // spoofed/arbitrary client ContentType is rejected before the bytes are
        // stored. (Magic-byte verification is the v2 hardening.)
        var contentType = (request.ContentType ?? string.Empty).Trim();
        if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException(
                "The file's declared content type does not match its extension.");

        // Magic-byte verification: the file's actual leading bytes must match the
        // declared extension, so a disguised binary (renamed executable, etc.) is
        // rejected regardless of the client-declared type — and independently of
        // whether the antivirus provider is active. Only runs on a seekable
        // stream (buffered upload); the position is restored for the scan/upload.
        if (request.Content.CanSeek && Signatures.TryGetValue(extension, out var signatures))
        {
            var header = new byte[12];
            request.Content.Position = 0;
            var read = await request.Content.ReadAsync(header.AsMemory(0, header.Length), ct).ConfigureAwait(false);
            request.Content.Position = 0;

            var matches = signatures.Any(sig =>
                read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));

            // WebP is "RIFF"....(4 bytes size)...."WEBP" — verify the WEBP tag too.
            if (matches && extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                matches = read >= 12 && header.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 });

            if (!matches)
                throw new ConflictException(
                    "The file's contents don't match its type (possible disguised or corrupt file).");
        }

        // A document may only be linked to one of the caller's own applications.
        if (request.ApplicationTrackerId is { } appId)
        {
            var ownsApplication = await db.Applications
                .AnyAsync(a => a.Id == appId && a.StudentId == userId, ct)
                .ConfigureAwait(false);
            if (!ownsApplication)
                throw new NotFoundException(nameof(ApplicationTracker), appId);
        }

        // Likewise a document may only be attached to one of the caller's own
        // paid review/support requests (the provider reviews it, but the student
        // owns it). Only while the request is still open for review.
        if (request.ScholarshipProviderReviewRequestId is { } crrId)
        {
            var ownsRequest = await db.ScholarshipProviderReviewRequests
                .AnyAsync(r => r.Id == crrId
                    && r.StudentId == userId
                    && (r.Status == ScholarshipProviderReviewRequestStatus.Submitted
                        || r.Status == ScholarshipProviderReviewRequestStatus.Pending
                        || r.Status == ScholarshipProviderReviewRequestStatus.UnderReview), ct)
                .ConfigureAwait(false);
            if (!ownsRequest)
                throw new NotFoundException(nameof(ScholarshipProviderReviewRequest), crrId);
        }

        var safeName = Path.GetFileName(request.FileName);

        // Antivirus scan BEFORE the bytes are ever persisted (security NFR).
        // Fail-closed: an infected file is rejected, and so is one that could
        // not be scanned at all — an unverified file is never stored.
        var scan = await fileScan.ScanAsync(request.Content, safeName, ct).ConfigureAwait(false);
        if (scan.Verdict == FileScanVerdict.Infected)
        {
            logger.LogWarning(
                "Document upload by {UserId} rejected — malware detected in {FileName}: {Detail}",
                userId, safeName, scan.Detail);
            throw new ConflictException($"File rejected — malware detected: {safeName}");
        }
        if (scan.Verdict == FileScanVerdict.ScanUnavailable)
        {
            logger.LogError(
                "Document upload by {UserId} rejected — {FileName} could not be virus-scanned: {Detail}",
                userId, safeName, scan.Detail);
            throw new ConflictException(
                "File could not be virus-scanned; upload rejected. Try again later.");
        }

        var storagePath = await storage
            .UploadAsync(request.Content, safeName, contentType, Container, ct)
            .ConfigureAwait(false);

        var now = clock.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            FileName = safeName,
            ContentType = contentType,
            SizeBytes = request.Length,
            StoragePath = storagePath,
            Category = request.Category,
            OnboardingType = request.Category == DocumentCategory.OnboardingDocument ? request.OnboardingType : null,
            UploadedAt = now,
            ApplicationTrackerId = request.ApplicationTrackerId,
            ScholarshipProviderReviewRequestId = request.ScholarshipProviderReviewRequestId,
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Document {DocumentId} uploaded by {UserId} ({Bytes} bytes).",
            document.Id, userId, request.Length);

        return DocumentMapping.ToDto(document);
    }
}
