using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequest : AuditableEntity
{
    public Guid UserId { get; set; }
    public UserRole RequestedRole { get; set; }
    public UpgradeRequestStatus Status { get; set; } = UpgradeRequestStatus.Pending;
    public string? AdminNotes { get; set; }
    public string? RejectionReason { get; set; }

    // rejection reasons 
    public string? RejectionReasons { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Consultant-specific fields
    public string? ExperienceSummary { get; set; }
    public string? ExpertiseTags { get; set; }
    public string? Languages { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }

    // Company-specific fields
    public string? CompanyName { get; set; }
    public string? CompanyCountry { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? CompanyRegistrationNumber { get; set; }

    // Proof document
    public string? ProofDocumentUrl { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
