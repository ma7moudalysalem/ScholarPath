using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Notification : AuditableEntity, ISoftDeletable
{
    public Guid RecipientUserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }

    public string TitleEn { get; set; } = default!;
    public string TitleAr { get; set; } = default!;
    public string BodyEn { get; set; } = default!;
    public string BodyAr { get; set; } = default!;
    public string? DeepLink { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public int Priority { get; set; } // 0=info, 1=normal, 2=high, 3=urgent

    // Idempotency: ensures event replays don't double-insert
    public string? IdempotencyKey { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }
    public bool DispatchSucceeded { get; set; }
    public string? DispatchError { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public class NotificationPreference : AuditableEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; } = true;
}
