using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ProfitShare.Queries.GetProfitShareAnalytics;

/// <summary>Aggregates captured-payment profit-share over a date window (PB-014 AC#6).</summary>
public sealed record GetProfitShareAnalyticsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<ProfitShareAnalyticsDto>;

public sealed class GetProfitShareAnalyticsQueryValidator
    : AbstractValidator<GetProfitShareAnalyticsQuery>
{
    public GetProfitShareAnalyticsQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'From' must be on or before 'To'.");
    }
}

public sealed class GetProfitShareAnalyticsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProfitShareAnalyticsQuery, ProfitShareAnalyticsDto>
{
    public async Task<ProfitShareAnalyticsDto> Handle(
        GetProfitShareAnalyticsQuery request, CancellationToken ct)
    {
        var to = request.To ?? DateTimeOffset.UtcNow;
        var from = request.From ?? to.AddMonths(-12);

        // Include PartiallyRefunded: those rows are still paid out to the
        // consultant (after the partial-refund payee recompute), so they belong
        // in profit-share analytics too.
        var rows = await db.Payments
            .AsNoTracking()
            .Where(p => (p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded)
                && p.CapturedAt != null
                && p.CapturedAt >= from
                && p.CapturedAt <= to)
            .Select(p => new
            {
                CapturedAt = p.CapturedAt!.Value,
                p.AmountCents,
                p.ProfitShareAmountCents,
                p.PayeeAmountCents,
            })
            .ToListAsync(ct);

        var monthly = rows
            .GroupBy(r => new { r.CapturedAt.Year, r.CapturedAt.Month })
            .Select(g => new ProfitShareMonthlyBucket(
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.ProfitShareAmountCents),
                g.Sum(x => x.AmountCents),
                g.Count()))
            .OrderBy(b => b.Year).ThenBy(b => b.Month)
            .ToList();

        return new ProfitShareAnalyticsDto(
            from,
            to,
            rows.Sum(r => r.AmountCents),
            rows.Sum(r => r.ProfitShareAmountCents),
            rows.Sum(r => r.PayeeAmountCents),
            rows.Count,
            monthly);
    }
}
