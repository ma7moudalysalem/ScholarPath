using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequest : AuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public UpgradeTarget Target { get; set; }
    public UpgradeRequestStatus Status { get; set; } = UpgradeRequestStatus.Pending;
    public string? Reason { get; set; }
    public string? ReviewerNotes { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? User { get; set; }
    public ICollection<UpgradeRequestFile> Files { get; } = [];
    public ICollection<UpgradeRequestLink> Links { get; } = [];
}

public class UpgradeRequestFile : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string FileName { get; set; } = default!;
    public string BlobUrl { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = default!;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UpgradeRequestLink : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string Label { get; set; } = default!;
    public string Url { get; set; } = default!;
}
