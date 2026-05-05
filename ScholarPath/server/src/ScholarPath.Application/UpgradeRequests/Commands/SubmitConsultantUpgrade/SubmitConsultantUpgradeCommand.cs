using MediatR;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;

public record SubmitConsultantUpgradeCommand(
    string ExperienceSummary,
    List<string> Languages,
    List<EducationEntryDto> Education,
    List<string> ExpertiseTags,
    List<UpgradeRequestLinkDto>? Links = null) : IRequest<SubmitUpgradeResponse>;

public record SubmitUpgradeResponse(Guid Id, string Status);
