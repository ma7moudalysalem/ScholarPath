namespace ScholarPath.Application.Scholarships.DTOs;

using ScholarPath.Domain.Enums;

public class ScholarshipSearchRequest
{
    public string? Search { get; set; }
    public string? Country { get; set; }
    public DegreeLevel? DegreeLevel { get; set; }
    public string? FieldOfStudy { get; set; }
    public ScholarshipFundingType? FundingType { get; set; }
    public DateTime? DeadlineFrom { get; set; }
    public DateTime? DeadlineTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public ScholarshipSortBy SortBy { get; set; } = ScholarshipSortBy.Relevance;
    public bool IncludeExpired { get; set; } = false;
}
