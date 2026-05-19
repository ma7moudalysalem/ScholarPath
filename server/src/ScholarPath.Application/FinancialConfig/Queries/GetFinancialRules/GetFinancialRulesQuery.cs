using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.FinancialConfig.Queries.GetFinancialRules;

/// <summary>
/// Lists financial-configuration rules, newest first, optionally narrowed by
/// payment type and/or status. With no filters it returns the full history
/// (FR-173/174) — Draft, Active and Archived rules alike.
/// </summary>
public sealed record GetFinancialRulesQuery(
    PaymentType? PaymentType = null,
    FinancialRuleStatus? Status = null) : IRequest<IReadOnlyList<FinancialConfigRuleDto>>;

public sealed class GetFinancialRulesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFinancialRulesQuery, IReadOnlyList<FinancialConfigRuleDto>>
{
    public async Task<IReadOnlyList<FinancialConfigRuleDto>> Handle(
        GetFinancialRulesQuery request, CancellationToken ct)
    {
        var query = db.FinancialConfigRules.AsNoTracking();

        if (request.PaymentType is { } type)
            query = query.Where(r => r.PaymentType == type);

        if (request.Status is { } status)
            query = query.Where(r => r.Status == status);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return rows
            .Select(r => new FinancialConfigRuleDto(
                r.Id, r.PaymentType, r.FeeKind, r.FeePercentage, r.FeeAmountCents,
                r.ProfitSharePercentage, r.Status, r.EffectiveFrom, r.EffectiveTo,
                r.SetByAdminId, r.Notes, r.CreatedAt, r.UpdatedAt))
            .ToList();
    }
}
