using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetScholarshipProviderInsights;

public record CountryBreakdownDto(
    string Country,
    int Count,
    decimal AcceptanceRate);

public record FieldBreakdownDto(
    string FieldEn,
    string FieldAr,
    int Count,
    decimal AcceptanceRate);

public record TopScholarshipDto(
    Guid Id,
    string Title,
    int Applications);

public record FunnelMonthDto(
    string Month,
    int Views,
    int Applied,
    int Accepted);

public record ScholarshipProviderInsightsDto(
    int TotalApplications,
    int SubmittedCount,
    int AcceptedCount,
    int RejectedCount,
    decimal AcceptanceRate,
    decimal AverageDaysToDecision,
    // Null for non-admin owners (the platform-wide delta is not disclosed to them —
    // DATA-05/FR-207). Only admins receive a real number, which may legitimately be 0.
    decimal? ComparisonToPlatformAvg,
    IReadOnlyList<CountryBreakdownDto> ByCountry,
    IReadOnlyList<FieldBreakdownDto> ByField,
    IReadOnlyList<TopScholarshipDto> TopScholarships,
    IReadOnlyList<FunnelMonthDto> MonthlyFunnel);

/// <summary>
/// Provider insights (PB-015 reports) — aggregates the application pipeline
/// across all scholarships owned by the supplied company, broken down by
/// country and field, with a comparison delta vs the platform-wide acceptance
/// rate. Authorisation: caller must be the owning company OR an admin.
/// </summary>
public record GetScholarshipProviderInsightsQuery(
    Guid? ScholarshipProviderId,
    DateOnly? From,
    DateOnly? To) : IRequest<ScholarshipProviderInsightsDto>;

public sealed class GetScholarshipProviderInsightsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipProviderInsightsQuery, ScholarshipProviderInsightsDto>
{
    public async Task<ScholarshipProviderInsightsDto> Handle(
        GetScholarshipProviderInsightsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var isAdmin = currentUser.IsAdminOrSuperAdmin();

        // Admins may supply a ScholarshipProviderId, owners default to themselves.
        var companyId = request.ScholarshipProviderId ?? userId;
        if (!isAdmin && companyId != userId)
            throw new ForbiddenAccessException("You may only view insights for your own company.");

        // Default window: last 365 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.From ?? today.AddDays(-365);
        var toDate = request.To ?? today;
        var fromOffset = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toOffset = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // Pull all applications tied to scholarships owned by this company, in
        // the window. We fan out from Applications so we can join Scholarship
        // (to filter by OwnerScholarshipProviderId and recover Category) and Student (to
        // recover CountryOfResidence) in one go.
        var applications = await db.Applications
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.CreatedAt >= fromOffset && a.CreatedAt <= toOffset)
            .Join(db.Scholarships.AsNoTracking().Where(s => s.OwnerScholarshipProviderId == companyId),
                a => a.ScholarshipId, s => s.Id,
                (a, s) => new { a, s })
            .Select(x => new
            {
                ApplicationId = x.a.Id,
                ScholarshipId = x.s.Id,
                ScholarshipTitle = x.s.TitleEn,
                CategoryId = x.s.CategoryId,
                StudentId = x.a.StudentId,
                Status = x.a.Status,
                SubmittedAt = x.a.SubmittedAt,
                DecisionAt = x.a.DecisionAt,
                CreatedAt = x.a.CreatedAt,
            })
            .ToListAsync(ct);

        var totalApplications = applications.Count;
        var submittedCount = applications.Count(a => a.SubmittedAt != null);
        var acceptedCount = applications.Count(a => a.Status == ApplicationStatus.Accepted);
        var rejectedCount = applications.Count(a => a.Status == ApplicationStatus.Rejected);
        var decided = submittedCount > 0
            ? applications.Count(a => a.Status == ApplicationStatus.Accepted
                                   || a.Status == ApplicationStatus.Rejected)
            : 0;
        var acceptanceRate = decided > 0
            ? Math.Round((decimal)acceptedCount * 100m / decided, 2)
            : 0m;

        // Avg days from SubmittedAt → DecisionAt
        var decisioned = applications
            .Where(a => a.SubmittedAt != null && a.DecisionAt != null)
            .ToList();
        var averageDaysToDecision = decisioned.Count > 0
            ? Math.Round((decimal)decisioned.Average(a =>
                (a.DecisionAt!.Value - a.SubmittedAt!.Value).TotalDays), 2)
            : 0m;

        // DATA-05 (FR-207 competitive isolation): the platform-wide acceptance-rate
        // delta is competitively sensitive — a ScholarshipProvider already receives
        // its own AcceptanceRate in this DTO, so exposing the delta lets it invert
        // the platform-wide average (and infer competitors' aggregate performance).
        // Only admins may see the delta; non-admin owners get null (no platform
        // figure disclosed — the client must not render a fabricated "at-average"
        // chip). This also skips the extra aggregate query for them.
        decimal? comparisonDelta = null;
        if (isAdmin)
        {
            var platformDecisionCounts = await db.Applications
                .AsNoTracking()
                .Where(a => !a.IsDeleted
                    && a.CreatedAt >= fromOffset
                    && a.CreatedAt <= toOffset
                    && (a.Status == ApplicationStatus.Accepted
                        || a.Status == ApplicationStatus.Rejected))
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Decided = g.Count(),
                    Accepted = g.Count(a => a.Status == ApplicationStatus.Accepted),
                })
                .FirstOrDefaultAsync(ct);
            var platformAcceptanceRate = platformDecisionCounts != null && platformDecisionCounts.Decided > 0
                ? Math.Round((decimal)platformDecisionCounts.Accepted * 100m / platformDecisionCounts.Decided, 2)
                : 0m;
            comparisonDelta = Math.Round(acceptanceRate - platformAcceptanceRate, 2);
        }

        // ── By country (resolved via student profile) ──────────────────────────
        var studentIds = applications.Select(a => a.StudentId).Distinct().ToList();
        var studentCountries = await db.Users
            .AsNoTracking()
            .Where(u => studentIds.Contains(u.Id))
            .Select(u => new { u.Id, u.CountryOfResidence })
            .ToListAsync(ct);
        var countryByStudent = studentCountries
            .ToDictionary(s => s.Id, s => string.IsNullOrWhiteSpace(s.CountryOfResidence) ? "Unknown" : s.CountryOfResidence!);

        var byCountry = applications
            .GroupBy(a => countryByStudent.TryGetValue(a.StudentId, out var c) ? c : "Unknown")
            .Select(g =>
            {
                var dec = g.Count(a => a.Status == ApplicationStatus.Accepted
                                    || a.Status == ApplicationStatus.Rejected);
                var acc = g.Count(a => a.Status == ApplicationStatus.Accepted);
                return new CountryBreakdownDto(
                    g.Key,
                    g.Count(),
                    dec > 0 ? Math.Round((decimal)acc * 100m / dec, 2) : 0m);
            })
            .OrderByDescending(c => c.Count)
            .Take(10)
            .ToList();

        // ── By field (via Scholarship.CategoryId → Category) ───────────────────
        var categoryIds = applications
            .Where(a => a.CategoryId != null)
            .Select(a => a.CategoryId!.Value)
            .Distinct()
            .ToList();
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.NameEn, c.NameAr })
            .ToListAsync(ct);
        var catLookup = categories.ToDictionary(c => c.Id, c => (En: c.NameEn, Ar: c.NameAr));

        var byField = applications
            .GroupBy(a => a.CategoryId)
            .Select(g =>
            {
                var en = "Uncategorized";
                var ar = "غير مصنفة";
                if (g.Key != null && catLookup.TryGetValue(g.Key.Value, out var c))
                {
                    en = c.En;
                    ar = c.Ar;
                }
                var dec = g.Count(a => a.Status == ApplicationStatus.Accepted
                                    || a.Status == ApplicationStatus.Rejected);
                var acc = g.Count(a => a.Status == ApplicationStatus.Accepted);
                return new FieldBreakdownDto(
                    en, ar,
                    g.Count(),
                    dec > 0 ? Math.Round((decimal)acc * 100m / dec, 2) : 0m);
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        // ── Top scholarships by application count ──────────────────────────────
        var topScholarships = applications
            .GroupBy(a => new { a.ScholarshipId, a.ScholarshipTitle })
            .Select(g => new TopScholarshipDto(g.Key.ScholarshipId, g.Key.ScholarshipTitle, g.Count()))
            .OrderByDescending(s => s.Applications)
            .Take(10)
            .ToList();

        // ── Monthly funnel: views (recommendation clicks) → applied → accepted
        var scholarshipIds = applications.Select(a => a.ScholarshipId).Distinct().ToList();
        var ownedScholarships = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.OwnerScholarshipProviderId == companyId)
            .Select(s => s.Id)
            .ToListAsync(ct);
        // Use OWNED set (broader than just scholarships that had apps) so a
        // scholarship with views-but-no-apps still contributes to the funnel.
        var viewsByMonth = await db.RecommendationClickEvents
            .AsNoTracking()
            .Where(r => ownedScholarships.Contains(r.ScholarshipId)
                && r.ClickedAt >= fromOffset
                && r.ClickedAt <= toOffset)
            .GroupBy(r => new
            {
                Year = r.ClickedAt.Year,
                Month = r.ClickedAt.Month,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
            })
            .ToListAsync(ct);

        var appliedByMonth = applications
            .Where(a => a.SubmittedAt != null)
            .GroupBy(a => new
            {
                a.SubmittedAt!.Value.Year,
                a.SubmittedAt!.Value.Month,
            })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToList();

        var acceptedByMonth = applications
            .Where(a => a.Status == ApplicationStatus.Accepted && a.DecisionAt != null)
            .GroupBy(a => new
            {
                a.DecisionAt!.Value.Year,
                a.DecisionAt!.Value.Month,
            })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToList();

        var months = viewsByMonth.Select(m => new { m.Year, m.Month })
            .Union(appliedByMonth.Select(m => new { m.Year, m.Month }))
            .Union(acceptedByMonth.Select(m => new { m.Year, m.Month }))
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        var monthlyFunnel = months.Select(m =>
        {
            var v = viewsByMonth.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            var a = appliedByMonth.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            var c = acceptedByMonth.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            return new FunnelMonthDto(
                $"{m.Year:0000}-{m.Month:00}",
                v?.Count ?? 0,
                a?.Count ?? 0,
                c?.Count ?? 0);
        }).ToList();

        return new ScholarshipProviderInsightsDto(
            TotalApplications:       totalApplications,
            SubmittedCount:          submittedCount,
            AcceptedCount:           acceptedCount,
            RejectedCount:           rejectedCount,
            AcceptanceRate:          acceptanceRate,
            AverageDaysToDecision:   averageDaysToDecision,
            ComparisonToPlatformAvg: comparisonDelta,
            ByCountry:               byCountry,
            ByField:                 byField,
            TopScholarships:         topScholarships,
            MonthlyFunnel:           monthlyFunnel);
    }
}
