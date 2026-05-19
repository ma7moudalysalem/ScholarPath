using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.FinancialConfig.Commands.ActivateFinancialRule;

/// <summary>
/// Activates a Draft financial-configuration rule. Any rule currently Active for
/// the same payment type is archived in the same transaction, so the FR-170
/// invariant — at most one Active rule per payment type — always holds.
/// </summary>
[Auditable(AuditAction.Update, "FinancialConfigRule",
    TargetIdProperty = nameof(RuleId),
    SummaryTemplate = "Activated financial rule {RuleId}")]
public sealed record ActivateFinancialRuleCommand(Guid RuleId) : IRequest;

public sealed class ActivateFinancialRuleCommandValidator
    : AbstractValidator<ActivateFinancialRuleCommand>
{
    public ActivateFinancialRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class ActivateFinancialRuleCommandHandler(
    IApplicationDbContext db,
    IDateTimeService clock) : IRequestHandler<ActivateFinancialRuleCommand>
{
    public async Task Handle(ActivateFinancialRuleCommand request, CancellationToken ct)
    {
        var rule = await db.FinancialConfigRules
            .FirstOrDefaultAsync(r => r.Id == request.RuleId, ct)
            ?? throw new NotFoundException(nameof(FinancialConfigRule), request.RuleId);

        if (rule.Status != FinancialRuleStatus.Draft)
        {
            throw new ConflictException("Only a draft financial rule can be activated.");
        }

        var now = clock.UtcNow;

        // FR-170: retire the rule currently in force for this payment type.
        var currentlyActive = await db.FinancialConfigRules
            .FirstOrDefaultAsync(
                r => r.PaymentType == rule.PaymentType && r.Status == FinancialRuleStatus.Active, ct);

        if (currentlyActive is not null)
        {
            currentlyActive.Status = FinancialRuleStatus.Archived;
            currentlyActive.EffectiveTo ??= now;
            currentlyActive.UpdatedAt = now;
        }

        rule.Status = FinancialRuleStatus.Active;
        rule.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
    }
}
