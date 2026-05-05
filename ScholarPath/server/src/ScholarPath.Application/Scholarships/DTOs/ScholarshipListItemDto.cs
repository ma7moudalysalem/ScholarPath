namespace ScholarPath.Application.Scholarships.DTOs;

using ScholarPath.Domain.Enums;

public class ScholarshipListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderNameAr { get; set; }
    public string? Country { get; set; }
    public DegreeLevel DegreeLevel { get; set; }
    public ScholarshipFundingType FundingType { get; set; }
    public decimal? AwardAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? Deadline { get; set; }
    public int? DeadlineCountdownDays { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsSaved { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
