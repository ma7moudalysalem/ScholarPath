using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetConsultantEarningsTrend;

public record MonthlyEarningDto(
    string Month,           // YYYY-MM
    decimal GrossUsd,
    decimal NetUsd,
    int BookingCount);

public record ConsultantEarningsTrendDto(
    decimal TotalGrossUsd,
    decimal TotalNetUsd,
    decimal TotalRefundedUsd,
    IReadOnlyList<MonthlyEarningDto> MonthlyEarnings,
    decimal ProjectedNextMonth,
    decimal PeerAvgNetUsd,
    decimal YourPercentile,
    decimal UpcomingBookingRevenue);

/// <summary>
/// Consultant earnings trend (PB-015 reports) — gross/net earnings for the
/// caller over the supplied window, broken down by month, with a simple
/// linear projection for the next month and an anonymised percentile rank
/// against all other consultants on the platform. Pending future booking
/// revenue is reported separately so the consultant can pace their work.
/// </summary>
public record GetConsultantEarningsTrendQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<ConsultantEarningsTrendDto>;

public sealed class GetConsultantEarningsTrendQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetConsultantEarningsTrendQuery, ConsultantEarningsTrendDto>
{
    public async Task<ConsultantEarningsTrendDto> Handle(
        GetConsultantEarningsTrendQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Default window: last 365 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.From ?? today.AddDays(-365);
        var toDate = request.To ?? today;
        var fromOffset = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toOffset = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // ── My captured payments in window ─────────────────────────────────────
        var myPayments = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && p.Type == PaymentType.ConsultantBooking
                && p.PayeeUserId == userId
                && p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset)
            .Select(p => new
            {
                p.AmountCents,
                p.PayeeAmountCents,
                p.RefundedAmountCents,
                CapturedAt = p.CapturedAt!.Value,
            })
            .ToListAsync(ct);

        var totalGross = Math.Round(myPayments.Sum(p => (decimal)p.AmountCents) / 100m, 2);
        var totalNet = Math.Round(myPayments.Sum(p => (decimal)p.PayeeAmountCents) / 100m, 2);
        var totalRefunded = Math.Round(myPayments.Sum(p => (decimal)p.RefundedAmountCents) / 100m, 2);

        // Monthly breakdown — also includes a count of distinct captured bookings
        var monthly = myPayments
            .GroupBy(p => new { p.CapturedAt.Year, p.CapturedAt.Month })
            .Select(g => new MonthlyEarningDto(
                $"{g.Key.Year:0000}-{g.Key.Month:00}",
                Math.Round(g.Sum(x => (decimal)x.AmountCents) / 100m, 2),
                Math.Round(g.Sum(x => (decimal)x.PayeeAmountCents) / 100m, 2),
                g.Count()))
            .OrderBy(m => m.Month, StringComparer.Ordinal)
            .ToList();

        // Simple linear projection: average net of the last 3 months × month-
        // over-month slope. If we have fewer than 2 months of data, fall back
        // to the mean (no slope).
        decimal projectedNextMonth = 0m;
        if (monthly.Count > 0)
        {
            var lastN = monthly.Skip(Math.Max(0, monthly.Count - 3)).ToList();
            if (lastN.Count == 1)
            {
                projectedNextMonth = lastN[0].NetUsd;
            }
            else
            {
                // Average net + average MoM delta (linear extrapolation)
                var avg = lastN.Average(m => m.NetUsd);
                decimal totalDelta = 0m;
                for (int i = 1; i < lastN.Count; i++)
                    totalDelta += lastN[i].NetUsd - lastN[i - 1].NetUsd;
                var avgDelta = totalDelta / (lastN.Count - 1);
                projectedNextMonth = Math.Max(0m, avg + avgDelta);
            }
        }
        projectedNextMonth = Math.Round(projectedNextMonth, 2);

        // ── Peer benchmark: average net earnings per consultant (in window) ────
        var peerStats = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && p.Type == PaymentType.ConsultantBooking
                && p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset
                && p.PayeeUserId != null)
            .GroupBy(p => p.PayeeUserId!.Value)
            .Select(g => new
            {
                ConsultantId = g.Key,
                NetCents = g.Sum(p => (decimal)p.PayeeAmountCents),
            })
            .ToListAsync(ct);

        var peerAvgNet = peerStats.Count > 0
            ? Math.Round(peerStats.Average(p => p.NetCents) / 100m, 2)
            : 0m;

        // Percentile: fraction of peers with strictly lower net earnings than me
        decimal yourPercentile;
        if (peerStats.Count == 0)
        {
            yourPercentile = 0m;
        }
        else
        {
            var myNetCents = totalNet * 100m;
            var below = peerStats.Count(p => p.ConsultantId != userId && p.NetCents < myNetCents);
            // Include myself in the denominator only if I have payments
            var denom = peerStats.Any(p => p.ConsultantId == userId)
                ? peerStats.Count
                : peerStats.Count + 1; // I'm a peer with zero net
            yourPercentile = denom > 0
                ? Math.Round((decimal)below * 100m / denom, 0)
                : 0m;
        }

        // ── Upcoming bookings: confirmed, scheduled in the future, payment not
        //    yet captured (held). Sums PriceUsd of those bookings.
        var nowOffset = DateTimeOffset.UtcNow;
        var upcomingRevenue = await db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.ConsultantId == userId
                && b.Status == BookingStatus.Confirmed
                && b.ScheduledStartAt > nowOffset)
            .SumAsync(b => (decimal?)b.PriceUsd, ct) ?? 0m;
        upcomingRevenue = Math.Round(upcomingRevenue, 2);

        return new ConsultantEarningsTrendDto(
            TotalGrossUsd:           totalGross,
            TotalNetUsd:             totalNet,
            TotalRefundedUsd:        totalRefunded,
            MonthlyEarnings:         monthly,
            ProjectedNextMonth:      projectedNextMonth,
            PeerAvgNetUsd:           peerAvgNet,
            YourPercentile:          yourPercentile,
            UpcomingBookingRevenue:  upcomingRevenue);
    }
}
