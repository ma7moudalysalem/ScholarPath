using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.FinancialConfig.Queries.GetFinancialRuleById;

/// <summary>Returns a single financial-configuration rule by id (FR-173).</summary>
public sealed record GetFinancialRuleByIdQuery(Guid RuleId)
    : IRequest<FinancialConfigRuleDto>;

public sealed class GetFinancialRuleByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFinancialRuleByIdQuery, FinancialConfigRuleDto>
{
    public async Task<FinancialConfigRuleDto> Handle(
        GetFinancialRuleByIdQuery request, CancellationToken ct)
    {
        var r = await db.FinancialConfigRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RuleId, ct)
            ?? throw new NotFoundException(nameof(FinancialConfigRule), request.RuleId);

        return new FinancialConfigRuleDto(
            r.Id, r.PaymentType, r.FeeKind, r.FeePercentage, r.FeeAmountCents,
            r.ProfitSharePercentage, r.Status, r.EffectiveFrom, r.EffectiveTo,
            r.SetByAdminId, r.Notes, r.CreatedAt, r.UpdatedAt);
    }
}
