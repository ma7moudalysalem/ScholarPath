using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.FinancialConfig.Commands.ArchiveFinancialRule;

/// <summary>
/// Archives a financial-configuration rule — the retire path. FR-176: a rule is
/// never hard-deleted (it may already be referenced by recorded transactions),
/// it is archived so the historical record is preserved.
/// </summary>
[Auditable(AuditAction.Update, "FinancialConfigRule",
    TargetIdProperty = nameof(RuleId),
    SummaryTemplate = "Archived financial rule {RuleId}")]
public sealed record ArchiveFinancialRuleCommand(Guid RuleId) : IRequest;

public sealed class ArchiveFinancialRuleCommandValidator
    : AbstractValidator<ArchiveFinancialRuleCommand>
{
    public ArchiveFinancialRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class ArchiveFinancialRuleCommandHandler(
    IApplicationDbContext db,
    IDateTimeService clock) : IRequestHandler<ArchiveFinancialRuleCommand>
{
    public async Task Handle(ArchiveFinancialRuleCommand request, CancellationToken ct)
    {
        var rule = await db.FinancialConfigRules
            .FirstOrDefaultAsync(r => r.Id == request.RuleId, ct)
            ?? throw new NotFoundException(nameof(FinancialConfigRule), request.RuleId);

        if (rule.Status == FinancialRuleStatus.Archived)
        {
            throw new ConflictException("This financial rule is already archived.");
        }

        var now = clock.UtcNow;
        if (rule.Status == FinancialRuleStatus.Active)
        {
            rule.EffectiveTo ??= now;
        }

        rule.Status = FinancialRuleStatus.Archived;
        rule.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
    }
}
