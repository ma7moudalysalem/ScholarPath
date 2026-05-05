using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.DTOs;

public class ApplicationListItemDto
{
    public Guid Id { get; set; }
    public Guid ScholarshipId { get; set; }
    public string ScholarshipTitle { get; set; } = string.Empty;
    public string? ScholarshipTitleAr { get; set; }
    public string? ProviderName { get; set; }
    public DateTime? Deadline { get; set; }
    public int? DeadlineCountdownDays { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? NotesPreview { get; set; }
    public bool HasReminders { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
