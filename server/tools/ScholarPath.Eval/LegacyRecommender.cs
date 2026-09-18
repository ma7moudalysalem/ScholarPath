using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;

namespace ScholarPath.Eval;

/// <summary>
/// The recommendation rule as it stood before the scoring redesign, kept here so
/// the "before" column of the evaluation is measured against the same eligibility
/// ground truth as the "after" column rather than against its own, older checker.
///
/// This is a verbatim transcription of <c>GenerateRecommendationsAsync</c> at
/// commit 789968f: additive points (level 30, exact-string field overlap 40,
/// country overlap 25, funding 5), no level gate, ties broken by title.
/// </summary>
public sealed class LegacyRecommender(ApplicationDbContext db)
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    public async Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId, int topN, CancellationToken ct)
    {
        topN = Math.Clamp(topN, 1, 20);

        var profile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        var preferredFields = ParseJsonArray(profile?.PreferredFieldsJson);
        var preferredCountries = ParseJsonArray(profile?.PreferredCountriesJson);
        var userLevel = profile?.AcademicLevel;

        var candidates = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline > DateTimeOffset.UtcNow)
            .Select(s => new
            {
                s.Id,
                s.TitleEn,
                s.TitleAr,
                s.TargetLevel,
                s.TargetCountriesJson,
                s.FieldsOfStudyJson,
                s.FundingAmountUsd,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var scored = new List<(Guid Id, int Score, string TitleEn, string TitleAr)>(candidates.Count);

        foreach (var c in candidates)
        {
            var fosFields = ParseJsonArray(c.FieldsOfStudyJson);
            var countries = ParseJsonArray(c.TargetCountriesJson);

            var score = 0;

            if (userLevel.HasValue && userLevel.Value == c.TargetLevel) score += 30;
            score += OverlapScore(preferredFields, fosFields, maxPoints: 40);
            score += OverlapScore(NormalizeCountries(preferredCountries), NormalizeCountries(countries), maxPoints: 25);

            if (c.FundingAmountUsd >= 10_000) score += 5;

            scored.Add((c.Id, Math.Min(score, 100), c.TitleEn, c.TitleAr));
        }

        var top = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.TitleEn)
            .Take(topN)
            .Select(x => new AiRecommendationItem(
                ScholarshipId: x.Id,
                MatchScore: x.Score,
                ExplanationEn: BuildExplanationEn(x.Score, x.TitleEn),
                ExplanationAr: BuildExplanationAr(x.Score, x.TitleAr)))
            .ToList();

        return new AiRecommendationResult(top, Disclaimer, 40, 12 * top.Count);
    }

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<string> NormalizeCountries(IReadOnlyList<string> countries)
        => countries.Select(CountryNormalizer.ToKey).Where(k => k.Length > 0).ToList();

    private static int OverlapScore(IReadOnlyList<string> a, IReadOnlyList<string> b, int maxPoints)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var set = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        var hits = b.Count(x => set.Contains(x));
        if (hits == 0) return 0;
        return Math.Min(maxPoints, hits * (maxPoints / Math.Max(a.Count, 1)));
    }

    private static string BuildExplanationEn(int score, string title) => score switch
    {
        >= 80 => $"Strong match for your profile — '{title}' aligns with your preferred fields and level.",
        >= 50 => $"Partial match — '{title}' overlaps with some of your preferences.",
        >= 20 => $"Loose match — '{title}' touches areas you've listed; worth a quick look.",
        _ => $"Weak match — '{title}' doesn't align well with your current profile.",
    };

    private static string BuildExplanationAr(int score, string title) => score switch
    {
        >= 80 => $"توافق قوي مع ملفك الشخصي — '{title}' قريبة من اهتماماتك ومستواك الأكاديمي.",
        >= 50 => $"توافق جزئي — '{title}' تتقاطع مع بعض تفضيلاتك.",
        >= 20 => $"توافق محدود — '{title}' تلمس المجالات التي أضفتها، يستحق نظرة سريعة.",
        _ => $"توافق ضعيف — '{title}' لا تتماشى حالياً مع ملفك.",
    };
}
