using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ApplicationTracker : AuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid ScholarshipId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Planned;
    public string? Notes { get; set; }
    public string? ChecklistJson { get; set; } // JSON array of {text, isChecked}
    public string? RemindersJson { get; set; } // JSON array of reminder presets
    public bool RemindersPaused { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Scholarship Scholarship { get; set; } = null!;
}
