using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.FinancialConfig.Commands.UpdateFinancialRule;

/// <summary>
/// Edits a financial-configuration rule. Only a Draft rule can be edited — an
/// Active or Archived rule is immutable, so the audit trail of what was in
/// force stays accurate (FR-171).
/// </summary>
[Auditable(AuditAction.Update, "FinancialConfigRule",
    TargetIdProperty = nameof(RuleId),
    SummaryTemplate = "Updated financial rule {RuleId}")]
public sealed record UpdateFinancialRuleCommand(
    Guid RuleId,
    FeeKind FeeKind,
    decimal? FeePercentage,
    long? FeeAmountCents,
    decimal ProfitSharePercentage,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Notes) : IRequest;

public sealed class UpdateFinancialRuleCommandValidator : AbstractValidator<UpdateFinancialRuleCommand>
{
    public UpdateFinancialRuleCommandValidator()
    {
        RuleFor(x => x.RuleId).NotEmpty();
        RuleFor(x => x.FeeKind).IsInEnum();

        RuleFor(x => x.FeePercentage)
            .NotNull().InclusiveBetween(0m, 1m)
            .When(x => x.FeeKind == FeeKind.Percentage)
            .WithMessage("Fee percentage must be a fraction between 0 and 1 (e.g. 0.10 = 10%).");

        RuleFor(x => x.FeeAmountCents)
            .NotNull().GreaterThan(0)
            .When(x => x.FeeKind == FeeKind.FixedAmount)
            .WithMessage("Fixed fee amount must be greater than zero.");

        RuleFor(x => x.ProfitSharePercentage)
            .InclusiveBetween(0m, 1m)
            .WithMessage("Profit-share must be a fraction between 0 and 1.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Effective-to date must be after the effective-from date.");

        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateFinancialRuleCommandHandler(
    IApplicationDbContext db,
    IDateTimeService clock) : IRequestHandler<UpdateFinancialRuleCommand>
{
    public async Task Handle(UpdateFinancialRuleCommand request, CancellationToken ct)
    {
        var rule = await db.FinancialConfigRules
            .FirstOrDefaultAsync(r => r.Id == request.RuleId, ct)
            ?? throw new NotFoundException(nameof(FinancialConfigRule), request.RuleId);

        if (rule.Status != FinancialRuleStatus.Draft)
        {
            throw new ConflictException("Only a draft financial rule can be edited.");
        }

        rule.FeeKind = request.FeeKind;
        rule.FeePercentage = request.FeeKind == FeeKind.Percentage ? request.FeePercentage : null;
        rule.FeeAmountCents = request.FeeKind == FeeKind.FixedAmount ? request.FeeAmountCents : null;
        rule.ProfitSharePercentage = request.ProfitSharePercentage;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.Notes = request.Notes;
        rule.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
