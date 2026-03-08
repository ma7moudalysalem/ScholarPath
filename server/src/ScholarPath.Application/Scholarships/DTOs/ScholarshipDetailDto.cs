namespace ScholarPath.Application.Scholarships.DTOs;

using ScholarPath.Domain.Enums;

public class ScholarshipDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderNameAr { get; set; }
    public string? Country { get; set; }
    public string? FieldOfStudy { get; set; }
    public DegreeLevel DegreeLevel { get; set; }
    public ScholarshipFundingType FundingType { get; set; }
    public decimal? AwardAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? Deadline { get; set; }
    public int? DeadlineCountdownDays { get; set; }
    public string? EligibilityDescription { get; set; }
    public string? RequiredDocuments { get; set; }
    public string? OverviewHtml { get; set; }
    public string? HowToApplyHtml { get; set; }
    public string? DocumentsChecklist { get; set; }
    public string? OfficialLink { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? MinGPA { get; set; }
    public int? MaxAge { get; set; }
    public string? EligibleCountries { get; set; }
    public string? EligibleMajors { get; set; }
    public string? Tags { get; set; }
    public int ViewCount { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsSaved { get; set; }
    public bool IsTracked { get; set; }
    public DateTime CreatedAt { get; set; }
}
