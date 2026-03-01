namespace ScholarPath.Application.Auth.DTOs;

public record CompanyUpgradeRequest(
    string CompanyName,
    string CompanyCountry,
    string? CompanyWebsite,
    string ContactPersonName,
    string ContactEmail,
    string? ContactPhone,
    string CompanyRegistrationNumber,
    string? ProofDocumentUrl
);
