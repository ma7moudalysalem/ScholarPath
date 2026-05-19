using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.FinancialConfig.Queries.PreviewFinancialCalculation;

/// <summary>
/// Simulates how a gross transaction amount would be split under a financial
/// rule (FR-167/175) — letting an admin sanity-check a rule before activating
/// it. Supply <see cref="RuleId"/> to preview one specific rule (e.g. a Draft),
/// or just <see cref="PaymentType"/> to preview the rule currently in force.
/// </summary>
public sealed record PreviewFinancialCalculationQuery(
    long GrossAmountCents,
    PaymentType? PaymentType = null,
    Guid? RuleId = null) : IRequest<FinancialCalculationPreviewDto>;

public sealed class PreviewFinancialCalculationQueryValidator
    : AbstractValidator<PreviewFinancialCalculationQuery>
{
    public PreviewFinancialCalculationQueryValidator()
    {
        RuleFor(x => x.GrossAmountCents)
            .GreaterThan(0)
            .WithMessage("Gross amount must be greater than zero.");

        RuleFor(x => x)
            .Must(x => x.RuleId.HasValue || x.PaymentType.HasValue)
            .WithMessage("Either a rule id or a payment type must be supplied.");
    }
}

public sealed class PreviewFinancialCalculationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<PreviewFinancialCalculationQuery, FinancialCalculationPreviewDto>
{
    public async Task<FinancialCalculationPreviewDto> Handle(
        PreviewFinancialCalculationQuery request, CancellationToken ct)
    {
        FinancialConfigRule? rule;

        if (request.RuleId is { } ruleId)
        {
            rule = await db.FinancialConfigRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == ruleId, ct)
                ?? throw new NotFoundException(nameof(FinancialConfigRule), ruleId);
        }
        else
        {
            rule = await db.FinancialConfigRules
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.PaymentType == request.PaymentType!.Value
                      && r.Status == FinancialRuleStatus.Active, ct);
        }

        var paymentType = rule?.PaymentType ?? request.PaymentType!.Value;

        // No rule on file for this payment type — fall back to a zero fee and the
        // legacy default profit-share so the preview still returns a sane figure.
        if (rule is null)
        {
            var fallback = FinancialCalculator.Calculate(
                request.GrossAmountCents, FeeKind.Percentage, 0m, null,
                ProfitShareCalculator.DefaultPercentage(paymentType));

            return new FinancialCalculationPreviewDto(
                null, paymentType, request.GrossAmountCents, FeeKind.Percentage,
                fallback.FeeCents, fallback.ProfitShareCents, fallback.PlatformTotalCents,
                fallback.PayeeNetCents, fallback.EffectiveFeeRate,
                fallback.PayeeNetCents >= 0, UsedFallback: true);
        }

        var b = FinancialCalculator.Calculate(request.GrossAmountCents, rule);

        return new FinancialCalculationPreviewDto(
            rule.Id, paymentType, request.GrossAmountCents, rule.FeeKind,
            b.FeeCents, b.ProfitShareCents, b.PlatformTotalCents, b.PayeeNetCents,
            b.EffectiveFeeRate, b.PayeeNetCents >= 0, UsedFallback: false);
    }
}
