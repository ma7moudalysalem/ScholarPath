using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A single platform-wide configuration value, keyed by a unique dotted string
/// (PB-011). The store has a fixed key set seeded at startup — settings are
/// updated in place, never created at runtime.
/// </summary>
public class PlatformSetting : AuditableEntity
{
    /// <summary>Unique dotted key, e.g. <c>maintenance.enabled</c>.</summary>
    public string Key { get; set; } = default!;

    /// <summary>The value, always stored as a string and parsed per <see cref="ValueType"/>.</summary>
    public string Value { get; set; } = default!;

    public PlatformSettingType ValueType { get; set; }

    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }

    /// <summary>Grouping bucket for the admin UI, e.g. <c>Access</c>.</summary>
    public string Category { get; set; } = "General";

    /// <summary>The admin who last changed this value (null = seeded, never edited).</summary>
    public Guid? UpdatedByAdminId { get; set; }
}
