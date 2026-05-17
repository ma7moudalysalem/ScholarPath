using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ProfitShare.Queries.GetProfitShareHistory;

/// <summary>Returns every profit-share config row (newest first), optionally filtered by type.</summary>
public sealed record GetProfitShareHistoryQuery(PaymentType? PaymentType = null)
    : IRequest<IReadOnlyList<ProfitShareConfigDto>>;

public sealed class GetProfitShareHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProfitShareHistoryQuery, IReadOnlyList<ProfitShareConfigDto>>
{
    public async Task<IReadOnlyList<ProfitShareConfigDto>> Handle(
        GetProfitShareHistoryQuery request, CancellationToken ct)
    {
        var query = db.ProfitShareConfigs.AsNoTracking();

        if (request.PaymentType is { } type)
            query = query.Where(c => c.PaymentType == type);

        var rows = await query
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync(ct);

        return rows
            .Select(c => new ProfitShareConfigDto(
                c.Id, c.PaymentType, c.Percentage, c.EffectiveFrom, c.EffectiveTo,
                c.SetByAdminId, c.Notes, c.EffectiveTo == null))
            .ToList();
    }
}
