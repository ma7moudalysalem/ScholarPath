using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Scholarship : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderNameAr { get; set; }
    public string? Country { get; set; }
    public string? FieldOfStudy { get; set; }
    public ScholarshipFundingType FundingType { get; set; }
    public DegreeLevel DegreeLevel { get; set; }
    public ScholarshipStatus Status { get; set; } = ScholarshipStatus.Published;
    public decimal? AwardAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? Deadline { get; set; }
    public string? EligibilityDescription { get; set; }
    public string? RequiredDocuments { get; set; }
    public string? OfficialLink { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; }
    public ICollection<ScholarshipTag> Tags { get; set; } = [];
    public string? OverviewHtml { get; set; }
    public string? HowToApplyHtml { get; set; }
    public ICollection<ScholarshipDocumentChecklist> DocumentsChecklist { get; set; } = [];

    // Eligibility criteria
    public decimal? MinGPA { get; set; }
    public int? MaxAge { get; set; }
    public ICollection<ScholarshipEligibleCountry> EligibleCountries { get; set; } = [];
    public ICollection<ScholarshipEligibleMajor> EligibleMajors { get; set; } = [];

    // Category
    public Guid? CategoryId { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public Category? Category { get; set; }
    public ICollection<SavedScholarship> SavedScholarships { get; set; } = [];
}
