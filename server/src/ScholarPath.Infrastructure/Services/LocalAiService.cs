using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Deterministic, zero-network AI provider. Scores open scholarships against a
/// student's profile (field / country / level overlap) and drops eligibility
/// criteria out of the listing text so the UI has real data to render even
/// with the "Stub" provider selected.
///
/// The shape matches a real AI provider: inputs, outputs, token counts, cost.
/// Tokens/cost are synthetic (deterministic) so tests and dashboards still work.
/// </summary>
public sealed class LocalAiService(ApplicationDbContext db) : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    // Synthetic cost model so cost caps behave reasonably in stub mode.
    private const decimal FakeCostPerRecommendation = 0.0008m;
    private const decimal FakeCostPerEligibility = 0.0005m;
    private const decimal FakeCostPerChatTurn = 0.0003m;

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
                s.TagsJson,
                s.FundingAmountUsd,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var scored = new List<(Guid Id, int Score, string TitleEn, string TitleAr)>(candidates.Count);

        foreach (var c in candidates)
        {
            var tags = ParseJsonArray(c.TagsJson);
            var countries = ParseJsonArray(c.TargetCountriesJson);

            var score = 0;

            if (userLevel.HasValue && userLevel.Value == c.TargetLevel) score += 30;
            score += OverlapScore(preferredFields, tags, maxPoints: 40);
            score += OverlapScore(preferredCountries, countries, maxPoints: 25);

            // Funding bonus — bigger awards nudge up by a few points
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

        // Synthetic token accounting — 40 prompt + 12 per recommendation
        var promptTokens = 40;
        var completionTokens = 12 * top.Count;

        return new AiRecommendationResult(top, Disclaimer, promptTokens, completionTokens);
    }

    public async Task<AiEligibilityResult> CheckEligibilityAsync(
        Guid userId, Guid scholarshipId, CancellationToken ct)
    {
        var scholarship = await db.Scholarships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scholarshipId, ct)
            .ConfigureAwait(false);

        if (scholarship is null)
        {
            return new AiEligibilityResult(
                Array.Empty<AiEligibilityCriterion>(),
                "Scholarship not found.",
                Disclaimer);
        }

        var profile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        var criteria = new List<AiEligibilityCriterion>();

        // Academic level
        criteria.Add(new AiEligibilityCriterion(
            Name: "Academic level",
            StudentValue: profile?.AcademicLevel?.ToString() ?? "unknown",
            ListingRequirement: scholarship.TargetLevel.ToString(),
            Match: profile?.AcademicLevel == scholarship.TargetLevel ? "yes"
                : profile?.AcademicLevel is null ? "unknown" : "no"));

        // Country match (nullable)
        var listingCountries = ParseJsonArray(scholarship.TargetCountriesJson);
        var studentCountry = profile?.Nationality ?? string.Empty;
        criteria.Add(new AiEligibilityCriterion(
            Name: "Country",
            StudentValue: string.IsNullOrWhiteSpace(studentCountry) ? "unknown" : studentCountry,
            ListingRequirement: listingCountries.Count == 0 ? "any" : string.Join(", ", listingCountries),
            Match: listingCountries.Count == 0 ? "yes"
                : string.IsNullOrWhiteSpace(studentCountry) ? "unknown"
                : listingCountries.Any(c => string.Equals(c, studentCountry, StringComparison.OrdinalIgnoreCase)) ? "yes" : "no"));

        // Field of study (loose match over preferred fields / tags)
        var tags = ParseJsonArray(scholarship.TagsJson);
        var fos = profile?.FieldOfStudy ?? string.Empty;
        criteria.Add(new AiEligibilityCriterion(
            Name: "Field of study",
            StudentValue: string.IsNullOrWhiteSpace(fos) ? "unknown" : fos,
            ListingRequirement: tags.Count == 0 ? "any" : string.Join(", ", tags),
            Match: tags.Count == 0 ? "yes"
                : string.IsNullOrWhiteSpace(fos) ? "unknown"
                : tags.Any(t => t.Contains(fos, StringComparison.OrdinalIgnoreCase)
                             || fos.Contains(t, StringComparison.OrdinalIgnoreCase)) ? "yes" : "partial"));

        var matches = criteria.Count(c => c.Match == "yes");
        var summary = matches == criteria.Count
            ? "You appear to meet all listed criteria."
            : matches == 0
                ? "You do not appear to meet any of the core criteria yet."
                : $"You match {matches} of {criteria.Count} criteria. Review the partial/no items before applying.";

        return new AiEligibilityResult(criteria, summary, Disclaimer);
    }

    public Task<AiChatResponse> AskAsync(
        Guid userId, string sessionId, string message, CancellationToken ct)
    {
        var msg = (message ?? string.Empty).Trim();
        var reply = RouteChat(msg);
        var chars = reply.Length;
        var promptTokens = Math.Max(1, msg.Length / 4);
        var completionTokens = Math.Max(1, chars / 4);

        return Task.FromResult(new AiChatResponse(
            Message: reply,
            Disclaimer: Disclaimer,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            EstimatedCostUsd: FakeCostPerChatTurn));
    }

    // ─── helpers ──────────────────────────────────────────────────────────

    internal static decimal CostForRecommendations(int itemCount) =>
        FakeCostPerRecommendation * Math.Max(itemCount, 1);

    internal static decimal CostForEligibility() => FakeCostPerEligibility;

    internal static decimal CostForChat() => FakeCostPerChatTurn;

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(json);
            return arr ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

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
        _     => $"Weak match — '{title}' doesn't align well with your current profile.",
    };

    private static string BuildExplanationAr(int score, string title) => score switch
    {
        >= 80 => $"توافق قوي مع ملفك الشخصي — '{title}' قريبة من اهتماماتك ومستواك الأكاديمي.",
        >= 50 => $"توافق جزئي — '{title}' تتقاطع مع بعض تفضيلاتك.",
        >= 20 => $"توافق محدود — '{title}' تلمس المجالات التي أضفتها، يستحق نظرة سريعة.",
        _     => $"توافق ضعيف — '{title}' لا تتماشى حالياً مع ملفك.",
    };

    private static string RouteChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Tell me what you're looking for — scholarship suggestions, deadlines, or application tips.";

        static bool ContainsCi(string haystack, string needle) =>
            haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        if (ContainsCi(message, "deadline") || ContainsCi(message, "موعد"))
            return "Deadlines are shown on each scholarship page. Open the Scholarships tab, then sort by Deadline (nearest first).";

        if (ContainsCi(message, "eligib") || ContainsCi(message, "هل انا مؤهل"))
            return "Open any scholarship detail and tap \"Check eligibility\" — we'll compare your profile against the listed criteria.";

        if (ContainsCi(message, "recommend") || ContainsCi(message, "ترشح"))
            return "The Recommendations widget on your dashboard ranks open scholarships by fit. Update your profile's preferred fields to sharpen the list.";

        if (ContainsCi(message, "apply") || ContainsCi(message, "تقديم"))
            return "Applications live under the Applications tab. You can only keep one active application per scholarship at a time.";

        if (ContainsCi(message, "consult") || ContainsCi(message, "مستشار"))
            return "Consultants are under the Consultants tab. You can book a paid session; funds are held in escrow until the session completes.";

        return "I can help with scholarships, eligibility, deadlines, and applications. Ask me something more specific and I'll point you to the right place.";
    }
}
