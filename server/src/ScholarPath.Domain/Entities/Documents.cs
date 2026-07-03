using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A file in a user's personal document vault (FR-216). The bytes live in the
/// configured storage provider (Local / Azure Blob); this row holds metadata
/// only. A document may optionally be linked to an application so the same
/// vaulted file can back an in-app submission (FR-049).
/// </summary>
public class Document : AuditableEntity, ISoftDeletable
{
    /// <summary>The user who owns the document. Only the owner (or an admin) may view or delete it.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Original file name supplied at upload, sanitised.</summary>
    public string FileName { get; set; } = default!;

    /// <summary>MIME type reported by the uploader.</summary>
    public string ContentType { get; set; } = default!;

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Provider-specific storage key/URL the bytes were saved under.</summary>
    public string StoragePath { get; set; } = default!;

    /// <summary>Folder the owner filed the document under.</summary>
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;

    /// <summary>
    /// FR-ONB-12 — for <see cref="DocumentCategory.OnboardingDocument"/> uploads,
    /// the specific verification document this is (legal registration, CV, …).
    /// Null for non-onboarding documents and for legacy uploads made before typing.
    /// </summary>
    public OnboardingDocumentType? OnboardingType { get; set; }

    /// <summary>When the file was uploaded.</summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Optional link to an application this document supports.</summary>
    public Guid? ApplicationTrackerId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Owner { get; set; }
    public ApplicationTracker? Application { get; set; }
}
