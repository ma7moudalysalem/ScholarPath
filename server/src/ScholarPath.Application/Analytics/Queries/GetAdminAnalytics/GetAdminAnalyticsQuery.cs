using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetAdminAnalytics;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record FunnelDayDto(
    DateOnly ActivityDate,
    int Registrations,
    int OnboardingCompleted,
    int ApplicationsSubmitted,
    int ApplicationsAccepted);

public record FinanceDayDto(
    DateOnly ActivityDate,
    string RevenueStream,
    decimal GrossUsd,
    decimal ProfitShareUsd,
    decimal PayeeNetUsd,
    decimal RefundedUsd,
    int RefundCount);

public record AcceptanceFieldDto(
    string FieldEn,
    string FieldAr,
    int TotalApplications,
    int AcceptedApplications,
    decimal? AcceptanceRatePercent);

public record AdminAnalyticsDto(
    IReadOnlyList<FunnelDayDto> Funnel,
    IReadOnlyList<FinanceDayDto> Finance,
    IReadOnlyList<AcceptanceFieldDto> AcceptanceByField);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns admin-level platform analytics from three SQL views:
/// <c>vw_funnel_daily</c>, <c>vw_finance_daily</c>, and <c>vw_acceptance_rates</c>.
/// </summary>
/// <param name="Days">
/// Number of trailing calendar days to include for funnel and finance data.
/// Defaults to 30.
/// </param>
public record GetAdminAnalyticsQuery(int Days = 30) : IRequest<AdminAnalyticsDto>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetAdminAnalyticsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminAnalyticsQuery, AdminAnalyticsDto>
{
    // Private projection records — property names must match SELECT column aliases.

    private sealed record FunnelRow(
        DateOnly ActivityDate,
        int Registrations,
        int OnboardingCompleted,
        int ApplicationsSubmitted,
        int ApplicationsAccepted);

    private sealed record FinanceRow(
        DateOnly ActivityDate,
        string RevenueStream,
        decimal GrossUsd,
        decimal ProfitShareUsd,
        decimal PayeeNetUsd,
        decimal RefundedUsd,
        int RefundCount);

    private sealed record AcceptanceRow(
        string FieldEn,
        string FieldAr,
        int TotalApplications,
        int AcceptedApplications,
        decimal? AcceptanceRatePercent);

    public async Task<AdminAnalyticsDto> Handle(
        GetAdminAnalyticsQuery request, CancellationToken ct)
    {
        var days = request.Days > 0 ? request.Days : 30;

        // Run all three view queries concurrently.
        var funnelTask = db.Database
            .SqlQuery<FunnelRow>(
                $"""
                SELECT
                    ActivityDate,
                    Registrations,
                    OnboardingCompleted,
                    ApplicationsSubmitted,
                    ApplicationsAccepted
                FROM dbo.vw_funnel_daily
                WHERE ActivityDate >= DATEADD(day, -{days}, CAST(GETUTCDATE() AS date))
                ORDER BY ActivityDate
                """)
            .ToListAsync(ct);

        var financeTask = db.Database
            .SqlQuery<FinanceRow>(
                $"""
                SELECT
                    ActivityDate,
                    RevenueStream,
                    GrossUsd,
                    ProfitShareUsd,
                    PayeeNetUsd,
                    RefundedUsd,
                    RefundCount
                FROM dbo.vw_finance_daily
                WHERE ActivityDate >= DATEADD(day, -{days}, CAST(GETUTCDATE() AS date))
                ORDER BY ActivityDate, RevenueStream
                """)
            .ToListAsync(ct);

        var acceptanceTask = db.Database
            .SqlQuery<AcceptanceRow>(
                $"""
                SELECT TOP 10
                    FieldEn,
                    FieldAr,
                    SUM(TotalApplications)    AS TotalApplications,
                    SUM(AcceptedApplications) AS AcceptedApplications,
                    CASE
                        WHEN SUM(TotalApplications) = 0 THEN NULL
                        ELSE CAST(SUM(AcceptedApplications) * 100.0
                             / SUM(TotalApplications) AS decimal(5,2))
                    END AS AcceptanceRatePercent
                FROM dbo.vw_acceptance_rates
                GROUP BY FieldEn, FieldAr
                ORDER BY SUM(TotalApplications) DESC
                """)
            .ToListAsync(ct);

        await Task.WhenAll(funnelTask, financeTask, acceptanceTask);

        var funnel = funnelTask.Result
            .Select(r => new FunnelDayDto(
                r.ActivityDate, r.Registrations, r.OnboardingCompleted,
                r.ApplicationsSubmitted, r.ApplicationsAccepted))
            .ToList();

        var finance = financeTask.Result
            .Select(r => new FinanceDayDto(
                r.ActivityDate, r.RevenueStream, r.GrossUsd,
                r.ProfitShareUsd, r.PayeeNetUsd, r.RefundedUsd, r.RefundCount))
            .ToList();

        var acceptance = acceptanceTask.Result
            .Select(r => new AcceptanceFieldDto(
                r.FieldEn, r.FieldAr, r.TotalApplications,
                r.AcceptedApplications, r.AcceptanceRatePercent))
            .ToList();

        return new AdminAnalyticsDto(funnel, finance, acceptance);
    }
}
