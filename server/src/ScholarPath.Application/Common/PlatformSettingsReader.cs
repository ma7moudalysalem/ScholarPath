using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Common;

/// <summary>
/// Dotted keys for the platform settings handlers read directly.
/// Mirrors the seeded defaults in <c>DbSeeder.SeedPlatformSettingsAsync</c> —
/// keep both in sync when adding a new key.
/// </summary>
public static class PlatformSettingsKeys
{
    /// <summary>
    /// Master payments switch. When <c>false</c>, the platform runs in fully
    /// free mode: every scholarship fee and consultant fee is forced to 0,
    /// the Apply Now / Booking flows always take the free path (no Stripe),
    /// fee inputs are hidden on the client, and billing dashboards show a
    /// "payments disabled" banner. The two allow-free flags below become
    /// moot when this is off.
    /// </summary>
    public const string PaymentsEnabled = "payments.enabled";

    /// <summary>
    /// When <c>true</c>, a Company may set a scholarship's Review Service Fee
    /// to 0 (the listing is free and the Apply Now flow skips Stripe). When
    /// <c>false</c>, validators reject 0 and require a positive fee.
    /// </summary>
    public const string AllowFreeScholarships = "payments.allowFreeScholarships";

    /// <summary>
    /// When <c>true</c>, a Consultant may set their Session Fee to 0 (the
    /// session is free and the booking flow skips Stripe). When <c>false</c>,
    /// the consultant must charge a positive fee.
    /// </summary>
    public const string AllowFreeConsultantSessions = "payments.allowFreeConsultantSessions";
}

/// <summary>
/// Tiny static reader for boolean platform settings. Handlers that need a
/// single flag for an authorisation gate hit this once; for hotter paths
/// (e.g. middleware), cache at the call site as the maintenance flag does.
/// </summary>
public static class PlatformSettingsReader
{
    /// <summary>
    /// Returns the boolean value of a platform setting, or <paramref name="defaultValue"/>
    /// when the key is missing or unparseable. Reads with AsNoTracking — the
    /// caller is checking the value, not mutating it.
    /// </summary>
    public static async Task<bool> GetBooleanAsync(
        IApplicationDbContext db,
        string key,
        bool defaultValue,
        CancellationToken ct)
    {
        var value = await db.PlatformSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
