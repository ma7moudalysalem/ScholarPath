namespace ScholarPath.Application.Common;

/// <summary>
/// Resolves an IANA ("Africa/Cairo") or Windows ("Egypt Standard Time")
/// timezone id to a <see cref="TimeZoneInfo"/>, falling back to UTC for a
/// missing or unrecognised id. .NET's ICU-backed lookup accepts both id
/// styles on every platform, so consultant availability stored with an IANA
/// id resolves correctly on a Windows host too.
/// </summary>
public static class TimeZoneResolver
{
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz))
        {
            return tz;
        }

        return TimeZoneInfo.Utc;
    }
}
