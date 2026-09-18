using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;

namespace ScholarPath.Eval;

/// <summary>
/// The scoring rule of commit 68afdda, a revision that was never released: each
/// listing scored as the share of the weight it could *attain*, so a criterion the
/// listing left open dropped out of numerator and denominator alike. The harness
/// showed it tying 142 of 150 lists at one score, and it was replaced before
/// release. It is kept here, transcribed verbatim, so that the column reporting it
/// can be reproduced from the command line like the other two.
/// </summary>
public sealed class IntermediateRecommender(ApplicationDbContext db)
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";
    private const int LevelWeight = 30;
    private const int FieldWeight = 40;
    private const int CountryWeight = 25;
    private const int FundingBonusPoints = 5;
    private const decimal FundingBonusThresholdUsd = 10_000m;

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
                s.TargetLevel,
                s.TargetCountriesJson,
                s.FieldsOfStudyJson,
                s.FundingAmountUsd,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var scored = new List<(Guid Id, int Score, string TitleEn)>(candidates.Count);

        foreach (var c in candidates)
        {
            var listingFields = ParseJsonArray(c.FieldsOfStudyJson);
            var listingCountries = NormalizeCountries(ParseJsonArray(c.TargetCountriesJson));
            var wantedCountries = NormalizeCountries(preferredCountries);

            decimal earned = 0, attainable = 0;

            if (userLevel.HasValue)
            {
                attainable += LevelWeight;
                if (userLevel.Value == c.TargetLevel) earned += LevelWeight;
            }

            if (preferredFields.Count > 0 && listingFields.Count > 0)
            {
                attainable += FieldWeight;
                earned += FieldWeight * PreferenceShare(preferredFields, listingFields);
            }

            if (wantedCountries.Count > 0 && listingCountries.Count > 0)
            {
                attainable += CountryWeight;
                earned += CountryWeight * PreferenceShare(wantedCountries, listingCountries);
            }

            var score = attainable == 0
                ? 0
                : (int)Math.Round(100m * earned / attainable, MidpointRounding.AwayFromZero);

            if (c.FundingAmountUsd >= FundingBonusThresholdUsd && score > 0) score += FundingBonusPoints;

            scored.Add((c.Id, Math.Clamp(score, 0, 100), c.TitleEn));
        }

        var top = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.TitleEn)
            .Take(topN)
            .Select(x => new AiRecommendationItem(x.Id, x.Score, string.Empty, string.Empty))
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

    private static decimal PreferenceShare(IReadOnlyList<string> wanted, IReadOnlyList<string> offered)
    {
        if (wanted.Count == 0 || offered.Count == 0) return 0m;

        var exact = new HashSet<string>(offered, StringComparer.OrdinalIgnoreCase);
        var offeredWords = offered.Select(SignificantWords).ToList();

        decimal credit = 0;
        foreach (var w in wanted)
        {
            if (exact.Contains(w))
            {
                credit += 1m;
                continue;
            }

            var words = SignificantWords(w);
            if (words.Count > 0 && offeredWords.Any(o => o.Overlaps(words))) credit += 0.5m;
        }

        return Math.Min(1m, credit / wanted.Count);
    }

    private static readonly char[] FieldWordSeparators = [' ', '&', ',', '/', '-', '(', ')', '\t'];

    private static readonly HashSet<string> FieldStopWords =
        new(StringComparer.OrdinalIgnoreCase) { "and", "the", "for", "of", "other", "studies", "general" };

    private static HashSet<string> SignificantWords(string value)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value)) return words;

        foreach (var token in value.Split(FieldWordSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length >= 3 && !FieldStopWords.Contains(token)) words.Add(token);
        }

        return words;
    }
}
