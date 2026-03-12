using MediatR;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Commands.ResubmitUpgradeRequest;

public record ResubmitUpgradeRequestCommand(
    Guid Id,
    string? ExperienceSummary,
    List<string>? Languages,
    List<EducationEntryDto>? Education,
    List<string>? ExpertiseTags,
    List<UpgradeRequestLinkDto>? Links,
    
    // Company properties
    string? CompanyName,
    string? Country,
    string? Website,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactPhone,
    string? CompanyRegistrationNumber
) : IRequest<SubmitUpgradeResponse>;
