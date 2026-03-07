namespace ScholarPath.Application.UpgradeRequests.DTOs;

public record CompanyUpgradeRequest(
    string CompanyName,
    string Country,
    string? Website,
    string ContactPersonName,
    string ContactEmail,
    string? ContactPhone,
    string CompanyRegistrationNumber);
