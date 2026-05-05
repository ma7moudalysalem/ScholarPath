namespace ScholarPath.Application.UpgradeRequests.DTOs;

public record EducationEntryDto(
    string InstitutionName,
    string DegreeName,
    string FieldOfStudy,
    int StartYear,
    int? EndYear,
    bool IsCurrentlyStudying);

public record UpgradeRequestLinkDto(
    string Url,
    string Label);

public record ConsultantUpgradeRequest(
    List<EducationEntryDto> Education,
    string ExperienceSummary,
    List<string> ExpertiseTags,
    List<string> Languages,
    List<UpgradeRequestLinkDto>? Links);
