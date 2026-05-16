namespace ScholarPath.Application.Common.Models;

/// <summary>
/// App-level settings bound from the "App" configuration section. Lives in the
/// Application layer so handlers can depend on IOptions&lt;AppOptions&gt; without
/// referencing Infrastructure or IConfiguration directly (Clean Architecture).
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public base URL of the SPA — used to build email links.</summary>
    public string ClientUrl { get; set; } = "http://localhost:5173";
}
