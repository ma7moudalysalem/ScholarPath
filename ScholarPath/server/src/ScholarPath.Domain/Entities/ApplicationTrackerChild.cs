using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ApplicationTrackerChecklistItem : BaseEntity
{
    public Guid ApplicationTrackerId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }

    public ApplicationTracker ApplicationTracker { get; set; } = null!;
}

public class ApplicationTrackerReminder : BaseEntity
{
    public Guid ApplicationTrackerId { get; set; }
    public string ReminderType { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public bool IsSent { get; set; }

    public ApplicationTracker ApplicationTracker { get; set; } = null!;
}
