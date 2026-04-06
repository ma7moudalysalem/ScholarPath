using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; } // null for system actions
    public AuditAction Action { get; set; }
    public string TargetType { get; set; } = default!;
    public Guid? TargetId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
    public string? Summary { get; set; }
}

public class UserDataRequest : AuditableEntity
{
    public Guid UserId { get; set; }
    public UserDataRequestType Type { get; set; }
    public UserDataRequestStatus Status { get; set; } = UserDataRequestStatus.Pending;

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledProcessAt { get; set; } // 30-day cooling for Delete
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? DownloadUrl { get; set; } // for Export
    public DateTimeOffset? DownloadExpiresAt { get; set; }
    public string? FailureReason { get; set; }
}

public class SuccessStory : AuditableEntity, ISoftDeletable
{
    public Guid? StudentId { get; set; } // nullable — admin can curate anonymous stories
    public string AuthorDisplayName { get; set; } = default!;
    public string? AuthorImageUrl { get; set; }
    public string HeadlineEn { get; set; } = default!;
    public string HeadlineAr { get; set; } = default!;
    public string BodyEn { get; set; } = default!;
    public string BodyAr { get; set; } = default!;
    public string? ScholarshipNameEn { get; set; }
    public string? ScholarshipNameAr { get; set; }
    public string? CountryCode { get; set; }

    public bool IsApproved { get; set; }
    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
