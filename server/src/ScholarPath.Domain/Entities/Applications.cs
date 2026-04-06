using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ApplicationTracker : AuditableEntity, ISoftDeletable
{
    public Guid StudentId { get; set; }
    public Guid ScholarshipId { get; set; }
    public ApplicationMode Mode { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    // For in-app submissions
    public string? FormDataJson { get; set; } // student's answers to listing schema
    public string? AttachedDocumentsJson { get; set; } // references to uploaded blob URLs

    // For external listings
    public string? ExternalTrackingUrl { get; set; }
    public string? ExternalReferenceId { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public DateTimeOffset? ReviewStartedAt { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public string? DecisionReason { get; set; }
    public bool IsReadOnly { get; set; } // true on Accepted/Rejected (final)
    public bool IsActive =>
        Status is not (ApplicationStatus.Withdrawn or ApplicationStatus.Rejected or ApplicationStatus.Accepted);

    // Reminders / personal notes
    public DateTimeOffset? NextReminderAt { get; set; }
    public string? PersonalNotes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Student { get; set; }
    public Scholarship? Scholarship { get; set; }
    public ICollection<ApplicationTrackerChild> Children { get; } = [];
}

/// <summary>
/// Child rows: status-history entries, task checklists, reviewer notes, etc.
/// </summary>
public class ApplicationTrackerChild : BaseEntity
{
    public Guid ApplicationTrackerId { get; set; }
    public string ChildType { get; set; } = default!; // "StatusHistory", "Note", "Task", "Attachment"
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActorUserId { get; set; }
    public int SortOrder { get; set; }
}
