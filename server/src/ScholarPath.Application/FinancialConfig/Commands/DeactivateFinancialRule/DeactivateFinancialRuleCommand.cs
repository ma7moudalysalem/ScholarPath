using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.FinancialConfig.Commands.DeactivateFinancialRule;

/// <summary>
/// Takes an Active financial-configuration rule out of service, returning it to
/// Draft so it can be edited or re-activated later. The payment type is left
/// with no Active rule until another is activated.
/// </summary>
[Auditable(AuditAction.Update, "FinancialConfigRule",
    TargetIdProperty = nameof(RuleId),
    SummaryTemplate = "Deactivated financial rule {RuleId}")]
public sealed record DeactivateFinancialRuleCommand(Guid RuleId) : IRequest;

public sealed class DeactivateFinancialRuleCommandValidator
    : AbstractValidator<DeactivateFinancialRuleCommand>
{
    public DeactivateFinancialRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class DeactivateFinancialRuleCommandHandler(
    IApplicationDbContext db,
    IDateTimeService clock) : IRequestHandler<DeactivateFinancialRuleCommand>
{
    public async Task Handle(DeactivateFinancialRuleCommand request, CancellationToken ct)
    {
        var rule = await db.FinancialConfigRules
            .FirstOrDefaultAsync(r => r.Id == request.RuleId, ct)
            ?? throw new NotFoundException(nameof(FinancialConfigRule), request.RuleId);

        if (rule.Status != FinancialRuleStatus.Active)
        {
            throw new ConflictException("Only an active financial rule can be deactivated.");
        }

        rule.Status = FinancialRuleStatus.Draft;
        rule.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
