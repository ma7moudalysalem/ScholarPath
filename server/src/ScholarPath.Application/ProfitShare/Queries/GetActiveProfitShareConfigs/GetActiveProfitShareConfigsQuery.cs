using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.ProfitShare.Queries.GetActiveProfitShareConfigs;

/// <summary>Returns the currently-active profit-share config for every payment type (PB-014).</summary>
public sealed record GetActiveProfitShareConfigsQuery : IRequest<IReadOnlyList<ProfitShareConfigDto>>;

public sealed class GetActiveProfitShareConfigsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetActiveProfitShareConfigsQuery, IReadOnlyList<ProfitShareConfigDto>>
{
    public async Task<IReadOnlyList<ProfitShareConfigDto>> Handle(
        GetActiveProfitShareConfigsQuery request, CancellationToken ct)
    {
        var configs = await db.ProfitShareConfigs
            .AsNoTracking()
            .Where(c => c.EffectiveTo == null)
            .ToListAsync(ct);

        return configs
            .OrderBy(c => c.PaymentType)
            .Select(c => new ProfitShareConfigDto(
                c.Id, c.PaymentType, c.Percentage, c.EffectiveFrom, c.EffectiveTo,
                c.SetByAdminId, c.Notes, true))
            .ToList();
    }
}
