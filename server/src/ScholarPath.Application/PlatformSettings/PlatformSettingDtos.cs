using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.PlatformSettings;

/// <summary>A single platform-wide configuration value (PB-011).</summary>
public sealed record PlatformSettingDto(
    Guid Id,
    string Key,
    string Value,
    PlatformSettingType ValueType,
    string? DescriptionEn,
    string? DescriptionAr,
    string Category,
    DateTimeOffset? UpdatedAt);
