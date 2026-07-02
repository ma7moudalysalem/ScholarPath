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
    /// When <c>true</c>, a ScholarshipProvider may set a scholarship's Review Service Fee
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

    /// <summary>
    /// The Azure OpenAI fine-tuning job ID of the most recently started job.
    /// Stored when the admin kicks off a fine-tuning run from the admin panel.
    /// </summary>
    public const string FineTuningLastJobId = "ai.fineTuning.lastJobId";

    /// <summary>
    /// Last-known status string polled from Azure for <see cref="FineTuningLastJobId"/>
    /// (e.g. "running", "succeeded", "failed").
    /// </summary>
    public const string FineTuningLastJobStatus = "ai.fineTuning.lastJobStatus";

    /// <summary>
    /// The finished fine-tuned model name returned by Azure once the job succeeds
    /// (e.g. "ft:gpt-4o-mini-2024-07-18:scholarpath::..."). The admin creates a
    /// deployment from this model in Azure AI Foundry, then calls Activate.
    /// </summary>
    public const string FineTuningLastFinishedModel = "ai.fineTuning.lastFinishedModel";

    /// <summary>
    /// Deployment name of the currently active fine-tuned chat model (set by the
    /// admin after creating the Azure deployment). Overrides
    /// <c>Ai:AzureOpenAi:FineTunedDeploymentName</c> from appsettings when non-empty.
    /// </summary>
    public const string ActiveFineTunedDeploymentName = "ai.fineTuning.activeDeploymentName";
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
    /// <summary>
    /// Returns the string value of a platform setting, or <paramref name="defaultValue"/>
    /// when the key is missing.
    /// </summary>
    public static async Task<string?> GetStringAsync(
        IApplicationDbContext db,
        string key,
        string? defaultValue,
        CancellationToken ct)
    {
        var value = await db.PlatformSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Upserts (creates or replaces) a platform setting.
    /// </summary>
    public static async Task SetAsync(
        IApplicationDbContext db,
        string key,
        string value,
        CancellationToken ct)
    {
        var existing = await db.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            db.PlatformSettings.Add(new Domain.Entities.PlatformSetting
            {
                Key = key,
                Value = value,
                ValueType = Domain.Enums.PlatformSettingType.Text,
                Category = "AI",
            });
        }
        else
        {
            existing.Value = value;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

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
