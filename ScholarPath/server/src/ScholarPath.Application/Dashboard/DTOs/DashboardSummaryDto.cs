namespace ScholarPath.Application.Dashboard.DTOs;

using ScholarPath.Domain.Enums;

public class DashboardSummaryDto
{
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public List<UpcomingDeadlineDto> DeadlinesSoon { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];
}

public class UpcomingDeadlineDto
{
    public Guid ScholarshipId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? ProviderName { get; set; }
    public DateTime Deadline { get; set; }
    public int CountdownDays { get; set; }
    public ApplicationStatus Status { get; set; }
}
