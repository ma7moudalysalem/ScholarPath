using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Analytics.Queries.GetAdminAnalytics;

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

public record GetAdminAnalyticsQuery(int Days = 30) : IRequest<AdminAnalyticsDto>;

public sealed class GetAdminAnalyticsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminAnalyticsQuery, AdminAnalyticsDto>
{
    public async Task<AdminAnalyticsDto> Handle(
        GetAdminAnalyticsQuery request, CancellationToken ct)
    {
        var days = request.Days > 0 ? request.Days : 30;
        var since = DateTimeOffset.UtcNow.Date.AddDays(-days);
        var sinceOffset = new DateTimeOffset(since, TimeSpan.Zero);

        // Funnel — group by day from Users + Applications
        var regsByDay = await db.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.CreatedAt >= sinceOffset)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new
            {
                Day = g.Key,
                Registrations = g.Count(),
                OnboardingCompleted = g.Count(u => u.IsOnboardingComplete),
            })
            .ToListAsync(ct);

        var submittedByDay = await db.Applications
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.SubmittedAt != null && a.SubmittedAt >= sinceOffset)
            .GroupBy(a => a.SubmittedAt!.Value.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var acceptedByDay = await db.Applications
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.Status == ApplicationStatus.Accepted && a.DecisionAt != null && a.DecisionAt >= sinceOffset)
            .GroupBy(a => a.DecisionAt!.Value.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var allDays = regsByDay.Select(r => r.Day)
            .Union(submittedByDay.Select(s => s.Day))
            .Union(acceptedByDay.Select(a => a.Day))
            .OrderBy(d => d)
            .ToList();

        var funnel = allDays.Select(d =>
        {
            var reg = regsByDay.FirstOrDefault(x => x.Day == d);
            var sub = submittedByDay.FirstOrDefault(x => x.Day == d);
            var acc = acceptedByDay.FirstOrDefault(x => x.Day == d);
            return new FunnelDayDto(
                DateOnly.FromDateTime(d),
                reg?.Registrations ?? 0,
                reg?.OnboardingCompleted ?? 0,
                sub?.Count ?? 0,
                acc?.Count ?? 0);
        }).ToList();

        // Finance — Booking payments
        var bookingFin = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CapturedAt != null && p.CapturedAt >= sinceOffset)
            .GroupBy(p => p.CapturedAt!.Value.Date)
            .Select(g => new FinanceDayDto(
                DateOnly.FromDateTime(g.Key),
                "ConsultantBooking",
                g.Sum(p => (decimal)p.AmountCents) / 100m,
                g.Sum(p => (decimal)p.ProfitShareAmountCents) / 100m,
                g.Sum(p => (decimal)p.PayeeAmountCents) / 100m,
                g.Sum(p => (decimal)p.RefundedAmountCents) / 100m,
                g.Count(p => p.RefundedAmountCents > 0)))
            .ToListAsync(ct);

        // Finance — ScholarshipProvider review payments
        var reviewFin = await db.ScholarshipProviderReviewPayments
            .AsNoTracking()
            .Where(p => p.CapturedAt != null && p.CapturedAt >= sinceOffset)
            .GroupBy(p => p.CapturedAt!.Value.Date)
            .Select(g => new FinanceDayDto(
                DateOnly.FromDateTime(g.Key),
                "ScholarshipProviderReview",
                g.Sum(p => p.AmountUsd),
                g.Sum(p => p.ProfitShareAmountUsd),
                g.Sum(p => p.PayeeAmountUsd),
                g.Sum(p => p.RefundedAmountUsd ?? 0m),
                g.Count(p => (p.RefundedAmountUsd ?? 0m) > 0)))
            .ToListAsync(ct);

        var finance = bookingFin.Concat(reviewFin)
            .OrderBy(f => f.ActivityDate)
            .ThenBy(f => f.RevenueStream)
            .ToList();

        // Acceptance rates by field (top 10 by total apps)
        var acceptance = await db.Applications
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Join(db.Scholarships.AsNoTracking(), a => a.ScholarshipId, s => s.Id, (a, s) => new { a, s })
            .GroupJoin(db.Categories.AsNoTracking(),
                joined => joined.s.CategoryId, c => c.Id,
                (joined, cats) => new { joined.a, joined.s, cats })
            .SelectMany(x => x.cats.DefaultIfEmpty(),
                (x, c) => new
                {
                    FieldEn = c != null ? c.NameEn : "Uncategorized",
                    FieldAr = c != null ? c.NameAr : "غير مصنفة",
                    x.a.Status,
                })
            .GroupBy(x => new { x.FieldEn, x.FieldAr })
            .Select(g => new
            {
                g.Key.FieldEn,
                g.Key.FieldAr,
                Total = g.Count(),
                Accepted = g.Count(x => x.Status == ApplicationStatus.Accepted),
            })
            .OrderByDescending(g => g.Total)
            .Take(10)
            .ToListAsync(ct);

        var acceptanceDtos = acceptance.Select(a => new AcceptanceFieldDto(
            a.FieldEn,
            a.FieldAr,
            a.Total,
            a.Accepted,
            a.Total == 0 ? (decimal?)null : Math.Round((decimal)a.Accepted * 100m / a.Total, 2)))
            .ToList();

        return new AdminAnalyticsDto(funnel, finance, acceptanceDtos);
    }
}
