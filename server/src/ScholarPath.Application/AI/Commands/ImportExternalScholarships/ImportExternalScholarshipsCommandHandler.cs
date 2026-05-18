using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.ImportExternalScholarships;

/// <summary>
/// Imports the curated external scholarships dataset into the catalogue. The
/// rows are Admin-owned external listings (<see cref="ListingMode.ExternalUrl"/>)
/// and become part of the searchable catalogue and the RAG knowledge base.
/// </summary>
public sealed class ImportExternalScholarshipsCommandHandler(
    IApplicationDbContext db,
    IDatasetProvider datasets,
    ILogger<ImportExternalScholarshipsCommandHandler> logger)
    : IRequestHandler<ImportExternalScholarshipsCommand, DatasetImportResultDto>
{
    private const string DatasetName = "external-scholarships";
    private const string SlugPrefix = "ext-";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<DatasetImportResultDto> Handle(
        ImportExternalScholarshipsCommand request, CancellationToken ct)
    {
        var json = datasets.GetDatasetJson(DatasetName);
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("Dataset '{Dataset}' is not bundled; nothing to import.", DatasetName);
            return new DatasetImportResultDto(DatasetName, 0, 0, 0, 0);
        }

        ExternalDataset? dataset;
        try
        {
            dataset = JsonSerializer.Deserialize<ExternalDataset>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse the '{Dataset}' dataset.", DatasetName);
            return new DatasetImportResultDto(DatasetName, 0, 0, 0, 0);
        }

        var entries = dataset?.Scholarships ?? [];
        if (entries.Count == 0)
            return new DatasetImportResultDto(DatasetName, 0, 0, 0, 0);

        var slugs = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Key))
            .Select(e => Slug(e.Key))
            .ToList();

        // IgnoreQueryFilters so a re-import also matches a previously soft-deleted row.
        var existing = await db.Scholarships
            .IgnoreQueryFilters()
            .Where(s => slugs.Contains(s.Slug))
            .ToListAsync(ct).ConfigureAwait(false);
        var bySlug = existing.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        int created = 0, updated = 0, skipped = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.Key)
                || !Enum.TryParse<FundingType>(e.FundingType, ignoreCase: true, out var fundingType)
                || !Enum.TryParse<AcademicLevel>(e.TargetLevel, ignoreCase: true, out var level))
            {
                skipped++;
                continue;
            }

            var slug = Slug(e.Key);
            if (!seen.Add(slug)) { skipped++; continue; } // duplicate key in the dataset

            var deadline = NextDeadline(e.DeadlineMonth, now);

            if (bySlug.TryGetValue(slug, out var scholarship))
            {
                Apply(scholarship, e, fundingType, level, deadline);
                scholarship.IsDeleted = false;
                scholarship.UpdatedAt = now;
                updated++;
            }
            else
            {
                scholarship = new Scholarship
                {
                    Slug = slug,
                    Status = ScholarshipStatus.Open,
                    Mode = ListingMode.ExternalUrl,
                    OpenedAt = now,
                    CreatedAt = now,
                };
                Apply(scholarship, e, fundingType, level, deadline);
                db.Scholarships.Add(scholarship);
                created++;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Imported '{Dataset}' — {Created} created, {Updated} updated, {Skipped} skipped.",
            DatasetName, created, updated, skipped);

        return new DatasetImportResultDto(DatasetName, entries.Count, created, updated, skipped);
    }

    // Dataset keys are authored as lowercase kebab-case; slug lookups are
    // case-insensitive, so the trimmed key is used directly.
    private static string Slug(string key) => SlugPrefix + key.Trim();

    private static void Apply(
        Scholarship s, ExternalScholarship e,
        FundingType fundingType, AcademicLevel level, DateTimeOffset deadline)
    {
        decimal? amount = e.FundingAmountUsd > 0 ? e.FundingAmountUsd : null;

        s.TitleEn = e.TitleEn;
        s.TitleAr = e.TitleAr;
        s.DescriptionEn = e.DescriptionEn;
        s.DescriptionAr = e.DescriptionAr;
        s.FundingType = fundingType;
        s.FundingAmountUsd = amount;
        s.Currency = "USD";
        s.TargetLevel = level;
        s.TargetCountriesJson = JsonSerializer.Serialize(e.TargetCountries ?? []);
        s.TagsJson = JsonSerializer.Serialize(e.Tags ?? []);
        s.EligibilityRequirementsEn = e.EligibilityEn;
        s.EligibilityRequirementsAr = e.EligibilityAr;
        s.ExternalApplicationUrl = e.ExternalUrl;
        s.Deadline = deadline;
    }

    /// <summary>The next future occurrence of <paramref name="month"/> (mid-month).</summary>
    private static DateTimeOffset NextDeadline(int month, DateTimeOffset now)
    {
        var m = month is >= 1 and <= 12 ? month : 6;
        var candidate = new DateTimeOffset(now.Year, m, 15, 23, 59, 0, TimeSpan.Zero);
        return candidate <= now ? candidate.AddYears(1) : candidate;
    }

    private sealed record ExternalDataset(List<ExternalScholarship>? Scholarships);

    private sealed record ExternalScholarship(
        string Key,
        string TitleEn,
        string TitleAr,
        string DescriptionEn,
        string DescriptionAr,
        string FundingType,
        int FundingAmountUsd,
        string TargetLevel,
        List<string>? TargetCountries,
        List<string>? Tags,
        string? EligibilityEn,
        string? EligibilityAr,
        int DeadlineMonth,
        string? ExternalUrl);
}
