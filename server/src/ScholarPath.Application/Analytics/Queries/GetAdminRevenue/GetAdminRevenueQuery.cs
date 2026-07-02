using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Analytics.Queries.GetAdminRevenue;

public record RevenueMonthDto(
    string Month,           // YYYY-MM
    decimal GrossUsd,
    decimal NetUsd,
    decimal RefundedUsd);

public record TopConsultantDto(
    Guid Id,
    string Name,
    decimal RevenueUsd);

public record AdminRevenueDto(
    decimal TotalGrossUsd,
    decimal TotalProfitShareUsd,
    decimal TotalPayeeNetUsd,
    decimal TotalRefundedUsd,
    decimal RefundRate,
    decimal BookingRevenueUsd,
    decimal ReviewRevenueUsd,
    decimal MonthOverMonthGrowth,
    int RefundCount,
    int SuccessfulPaymentCount,
    IReadOnlyList<RevenueMonthDto> ByMonth,
    IReadOnlyList<TopConsultantDto> TopConsultants);

/// <summary>
/// Admin revenue report (PB-015 reports) — aggregates gross / profit-share /
/// payee-net / refund totals across Payments and ScholarshipProviderReviewPayments in a
/// caller-supplied date window. Returns a monthly breakdown, the top 5 earning
/// consultants, and a month-over-month growth percentage computed against the
/// equivalent prior period.
/// </summary>
public record GetAdminRevenueQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<AdminRevenueDto>;

public sealed class GetAdminRevenueQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminRevenueQuery, AdminRevenueDto>
{
    public async Task<AdminRevenueDto> Handle(
        GetAdminRevenueQuery request, CancellationToken ct)
    {
        // Default window: last 90 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.From ?? today.AddDays(-90);
        var toDate = request.To ?? today;

        var fromOffset = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toOffset = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // Prior period for MoM comparison (same length, immediately before)
        var windowDays = (toDate.DayNumber - fromDate.DayNumber) + 1;
        var priorFromOffset = fromOffset.AddDays(-windowDays);
        var priorToOffset = fromOffset.AddTicks(-1);

        // ── Booking payments (captured within window) ──────────────────────────
        var bookingTotals = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Gross = g.Sum(p => (decimal)p.AmountCents) / 100m,
                ProfitShare = g.Sum(p => (decimal)p.ProfitShareAmountCents) / 100m,
                PayeeNet = g.Sum(p => (decimal)p.PayeeAmountCents) / 100m,
                Refunded = g.Sum(p => (decimal)p.RefundedAmountCents) / 100m,
                RefundCount = g.Count(p => p.RefundedAmountCents > 0),
                SuccessfulCount = g.Count(),
            })
            .FirstOrDefaultAsync(ct);

        // ── ScholarshipProvider review payments (captured within window) ───────────────────
        var reviewTotals = await db.ScholarshipProviderReviewPayments
            .AsNoTracking()
            .Where(p => p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Gross = g.Sum(p => p.AmountUsd),
                ProfitShare = g.Sum(p => p.ProfitShareAmountUsd),
                PayeeNet = g.Sum(p => p.PayeeAmountUsd),
                Refunded = g.Sum(p => p.RefundedAmountUsd ?? 0m),
                RefundCount = g.Count(p => (p.RefundedAmountUsd ?? 0m) > 0),
                SuccessfulCount = g.Count(),
            })
            .FirstOrDefaultAsync(ct);

        var bookingGross = bookingTotals?.Gross ?? 0m;
        var reviewGross = reviewTotals?.Gross ?? 0m;
        var totalGross = bookingGross + reviewGross;
        var totalProfit = (bookingTotals?.ProfitShare ?? 0m) + (reviewTotals?.ProfitShare ?? 0m);
        var totalPayee = (bookingTotals?.PayeeNet ?? 0m) + (reviewTotals?.PayeeNet ?? 0m);
        var totalRefunded = (bookingTotals?.Refunded ?? 0m) + (reviewTotals?.Refunded ?? 0m);
        var refundCount = (bookingTotals?.RefundCount ?? 0) + (reviewTotals?.RefundCount ?? 0);
        var successCount = (bookingTotals?.SuccessfulCount ?? 0) + (reviewTotals?.SuccessfulCount ?? 0);
        var refundRate = totalGross > 0 ? Math.Round(totalRefunded / totalGross, 4) : 0m;

        // ── Prior-period gross for MoM comparison ──────────────────────────────
        var priorBookingGross = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && p.CapturedAt != null
                && p.CapturedAt >= priorFromOffset
                && p.CapturedAt <= priorToOffset)
            .SumAsync(p => (decimal?)p.AmountCents, ct) ?? 0m;
        priorBookingGross /= 100m;
        var priorReviewGross = await db.ScholarshipProviderReviewPayments
            .AsNoTracking()
            .Where(p => p.CapturedAt != null
                && p.CapturedAt >= priorFromOffset
                && p.CapturedAt <= priorToOffset)
            .SumAsync(p => (decimal?)p.AmountUsd, ct) ?? 0m;
        var priorGross = priorBookingGross + priorReviewGross;
        var growth = priorGross > 0
            ? Math.Round((totalGross - priorGross) / priorGross * 100m, 2)
            : (totalGross > 0 ? 100m : 0m);

        // ── Monthly breakdown ──────────────────────────────────────────────────
        var bookingByMonth = await db.Payments
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset)
            .GroupBy(p => new
            {
                Year = p.CapturedAt!.Value.Year,
                Month = p.CapturedAt!.Value.Month,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Gross = g.Sum(p => (decimal)p.AmountCents) / 100m,
                Net = g.Sum(p => (decimal)p.PayeeAmountCents) / 100m,
                Refunded = g.Sum(p => (decimal)p.RefundedAmountCents) / 100m,
            })
            .ToListAsync(ct);

        var reviewByMonth = await db.ScholarshipProviderReviewPayments
            .AsNoTracking()
            .Where(p => p.CapturedAt != null
                && p.CapturedAt >= fromOffset
                && p.CapturedAt <= toOffset)
            .GroupBy(p => new
            {
                Year = p.CapturedAt!.Value.Year,
                Month = p.CapturedAt!.Value.Month,
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Gross = g.Sum(p => p.AmountUsd),
                Net = g.Sum(p => p.PayeeAmountUsd),
                Refunded = g.Sum(p => p.RefundedAmountUsd ?? 0m),
            })
            .ToListAsync(ct);

        var byMonth = bookingByMonth
            .Concat(reviewByMonth)
            .GroupBy(x => new { x.Year, x.Month })
            .Select(g => new RevenueMonthDto(
                $"{g.Key.Year:0000}-{g.Key.Month:00}",
                Math.Round(g.Sum(x => x.Gross), 2),
                Math.Round(g.Sum(x => x.Net), 2),
                Math.Round(g.Sum(x => x.Refunded), 2)))
            .OrderBy(m => m.Month, StringComparer.Ordinal)
            .ToList();

        // ── Top 5 consultants by booking gross within window ───────────────────
        var topConsultantsRaw = await db.Payments
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
                RevenueCents = g.Sum(p => (decimal)p.AmountCents),
            })
            .OrderByDescending(x => x.RevenueCents)
            .Take(5)
            .ToListAsync(ct);

        var consultantIds = topConsultantsRaw.Select(x => x.ConsultantId).ToList();
        var consultants = await db.Users
            .AsNoTracking()
            .Where(u => consultantIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var topConsultants = topConsultantsRaw
            .Select(t =>
            {
                var u = consultants.FirstOrDefault(c => c.Id == t.ConsultantId);
                var name = u != null
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : t.ConsultantId.ToString();
                return new TopConsultantDto(
                    t.ConsultantId,
                    string.IsNullOrWhiteSpace(name) ? "Consultant" : name,
                    Math.Round(t.RevenueCents / 100m, 2));
            })
            .ToList();

        return new AdminRevenueDto(
            TotalGrossUsd:           Math.Round(totalGross, 2),
            TotalProfitShareUsd:     Math.Round(totalProfit, 2),
            TotalPayeeNetUsd:        Math.Round(totalPayee, 2),
            TotalRefundedUsd:        Math.Round(totalRefunded, 2),
            RefundRate:              refundRate,
            BookingRevenueUsd:       Math.Round(bookingGross, 2),
            ReviewRevenueUsd:        Math.Round(reviewGross, 2),
            MonthOverMonthGrowth:    growth,
            RefundCount:             refundCount,
            SuccessfulPaymentCount:  successCount,
            ByMonth:                 byMonth,
            TopConsultants:          topConsultants);
    }
}
