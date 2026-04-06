using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Category : AuditableEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? IconKey { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<Scholarship> Scholarships { get; } = [];
}

public class Scholarship : AuditableEntity, ISoftDeletable
{
    public string TitleEn { get; set; } = default!;
    public string TitleAr { get; set; } = default!;
    public string DescriptionEn { get; set; } = default!;
    public string DescriptionAr { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public Guid? CategoryId { get; set; }
    public Guid? OwnerCompanyId { get; set; } // null for Admin-created external listings
    public Guid? CreatedByAdminId { get; set; }

    public ListingMode Mode { get; set; } = ListingMode.InApp;
    public string? ExternalApplicationUrl { get; set; } // only when Mode=ExternalUrl

    public ScholarshipStatus Status { get; set; } = ScholarshipStatus.Draft;
    public DateTimeOffset Deadline { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }

    public FundingType FundingType { get; set; }
    public decimal? FundingAmountUsd { get; set; }
    public string? Currency { get; set; } = "USD";
    public AcademicLevel TargetLevel { get; set; }
    public string? TargetCountriesJson { get; set; }
    public string? EligibilityRequirementsEn { get; set; }
    public string? EligibilityRequirementsAr { get; set; }
    public string? TagsJson { get; set; }

    // JSON-serialized form schema + required docs for in-app listings
    public string? ApplicationFormSchemaJson { get; set; }
    public string? RequiredDocumentsJson { get; set; }

    public decimal? ReviewFeeUsd { get; set; } // Company's charge for reviewing

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public Category? Category { get; set; }
    public ApplicationUser? OwnerCompany { get; set; }
    public ICollection<ScholarshipChild> Children { get; } = [];
    public ICollection<SavedScholarship> Bookmarks { get; } = [];
    public ICollection<ApplicationTracker> Applications { get; } = [];
}

/// <summary>
/// Generic child row for scholarship auxiliary data (requirements lists, benefits lists, etc.).
/// Retained from legacy schema to preserve flexibility.
/// </summary>
public class ScholarshipChild : BaseEntity
{
    public Guid ScholarshipId { get; set; }
    public string ChildType { get; set; } = default!; // e.g., "Requirement", "Benefit", "RequiredDoc"
    public string KeyEn { get; set; } = default!;
    public string? KeyAr { get; set; }
    public string? ValueEn { get; set; }
    public string? ValueAr { get; set; }
    public int SortOrder { get; set; }
}

public class SavedScholarship : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ScholarshipId { get; set; }
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }
}
