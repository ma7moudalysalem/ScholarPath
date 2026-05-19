using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.FinancialConfig;

/// <summary>
/// Resolves the platform take and payee net for a payment (FR-163..176). The
/// active <see cref="Domain.Entities.FinancialConfigRule"/> for the payment type
/// governs when one exists; otherwise the resolver falls back to the legacy
/// <see cref="ProfitShareConfigResolver"/> (profit-share only, no separate fee),
/// so payments keep working until a financial rule is activated.
/// </summary>
public static class FinancialRuleResolver
{
    public static async Task<PaymentSplit> ResolvePaymentSplitAsync(
        IApplicationDbContext db,
        PaymentType type,
        long grossAmountCents,
        CancellationToken ct)
    {
        // At most one rule per type can be Active (a filtered unique index
        // enforces it), so the lookup is deterministic.
        var rule = await db.FinancialConfigRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.PaymentType == type && r.Status == FinancialRuleStatus.Active, ct)
            .ConfigureAwait(false);

        FinancialBreakdown breakdown;
        if (rule is not null)
        {
            breakdown = FinancialCalculator.Calculate(grossAmountCents, rule);
        }
        else
        {
            var pct = await ProfitShareConfigResolver
                .ResolveActivePercentageAsync(db, type, ct)
                .ConfigureAwait(false);
            breakdown = FinancialCalculator.Calculate(
                grossAmountCents, FeeKind.Percentage, 0m, null, pct);
        }

        // A fixed fee can exceed a small gross — cap the platform take at the
        // gross so the payee is never negative. The simulator flags this to the
        // admin (IsViable = false) at configuration time.
        var platformTake = Math.Min(breakdown.PlatformTotalCents, grossAmountCents);
        return new PaymentSplit(platformTake, grossAmountCents - platformTake);
    }
}

/// <summary>The platform take and payee net for a payment, summing to the gross.</summary>
public sealed record PaymentSplit(long PlatformTakeCents, long PayeeNetCents);
