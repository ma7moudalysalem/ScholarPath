namespace ScholarPath.Application.Auth.DTOs;

public record ConsultantUpgradeRequest(
    string ExperienceSummary,
    string ExpertiseTags,
    string Languages,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? ProofDocumentUrl
);
