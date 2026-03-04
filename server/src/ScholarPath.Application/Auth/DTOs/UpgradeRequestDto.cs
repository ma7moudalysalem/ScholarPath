using System;
using System.Collections.Generic;
using System.Text;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.DTOs;

public record UpgradeRequestDto(
    UserRole RequestedRole,
    string ExperienceSummary,
    List<string> Languages,
    List<EducationEntryDto> EducationEntries,
    List<LinkDto> Links,
    List<string> ExpertiseTags
);

public record EducationEntryDto(
    string InstitutionName,
    string DegreeName,
    string FieldOfStudy,
    int StartYear,
    int? EndYear,
    bool IsCurrentlyStudying
);

public record LinkDto(string Url, string Label);
