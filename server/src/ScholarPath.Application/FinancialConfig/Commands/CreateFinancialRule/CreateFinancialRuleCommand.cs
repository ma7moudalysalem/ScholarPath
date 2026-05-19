using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.FinancialConfig.Commands.CreateFinancialRule;

/// <summary>
/// Creates a new financial-configuration rule in Draft state (FR-165). The rule
/// only takes effect once it is activated.
/// </summary>
[Auditable(AuditAction.Create, "FinancialConfigRule",
    SummaryTemplate = "Created financial rule for {PaymentType}")]
public sealed record CreateFinancialRuleCommand(
    PaymentType PaymentType,
    FeeKind FeeKind,
    decimal? FeePercentage,
    long? FeeAmountCents,
    decimal ProfitSharePercentage,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Notes) : IRequest<Guid>;

public sealed class CreateFinancialRuleCommandValidator : AbstractValidator<CreateFinancialRuleCommand>
{
    public CreateFinancialRuleCommandValidator()
    {
        RuleFor(x => x.PaymentType).IsInEnum();
        RuleFor(x => x.FeeKind).IsInEnum();

        // FR-168: reject invalid / negative / out-of-policy values.
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

        // A percentage-fee rule must still leave something for the payee.
        RuleFor(x => x)
            .Must(x => (x.FeePercentage ?? 0m) + x.ProfitSharePercentage <= 1m)
            .When(x => x.FeeKind == FeeKind.Percentage)
            .WithMessage("Fee percentage and profit-share together cannot exceed 100%.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Effective-to date must be after the effective-from date.");

        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CreateFinancialRuleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock) : IRequestHandler<CreateFinancialRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateFinancialRuleCommand request, CancellationToken ct)
    {
        var adminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User identity could not be resolved.");

        var rule = new FinancialConfigRule
        {
            Id = Guid.NewGuid(),
            PaymentType = request.PaymentType,
            FeeKind = request.FeeKind,
            FeePercentage = request.FeeKind == FeeKind.Percentage ? request.FeePercentage : null,
            FeeAmountCents = request.FeeKind == FeeKind.FixedAmount ? request.FeeAmountCents : null,
            ProfitSharePercentage = request.ProfitSharePercentage,
            Status = FinancialRuleStatus.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            SetByAdminId = adminId,
            Notes = request.Notes,
            CreatedAt = clock.UtcNow,
        };

        db.FinancialConfigRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule.Id;
    }
}
