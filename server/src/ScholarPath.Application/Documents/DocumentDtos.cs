using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Documents;

/// <summary>A document-vault entry as exposed on the wire (FR-216). Never carries the bytes.</summary>
public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentCategory Category,
    DateTimeOffset UploadedAt,
    Guid? ApplicationTrackerId,
    // FR-ONB-12/14 — the onboarding document type, so the admin review screen can
    // label each verification file. Null for non-onboarding / legacy uploads.
    OnboardingDocumentType? OnboardingType = null);

/// <summary>The file bytes plus the metadata needed to stream a download.</summary>
public sealed record DocumentDownloadDto(
    Stream Content,
    string FileName,
    string ContentType);

/// <summary>Maps a <see cref="Document"/> entity to its metadata DTO.</summary>
public static class DocumentMapping
{
    public static DocumentDto ToDto(Document d) => new(
        d.Id, d.FileName, d.ContentType, d.SizeBytes, d.Category, d.UploadedAt, d.ApplicationTrackerId, d.OnboardingType);
}
