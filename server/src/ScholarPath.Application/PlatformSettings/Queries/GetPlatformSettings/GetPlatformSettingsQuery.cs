using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.PlatformSettings.Queries.GetPlatformSettings;

/// <summary>Returns every platform setting, grouped-friendly (Category then Key) order (PB-011).</summary>
public sealed record GetPlatformSettingsQuery : IRequest<IReadOnlyList<PlatformSettingDto>>;

public sealed class GetPlatformSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPlatformSettingsQuery, IReadOnlyList<PlatformSettingDto>>
{
    public async Task<IReadOnlyList<PlatformSettingDto>> Handle(
        GetPlatformSettingsQuery request, CancellationToken ct)
    {
        return await db.PlatformSettings
            .AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .Select(s => new PlatformSettingDto(
                s.Id, s.Key, s.Value, s.ValueType,
                s.DescriptionEn, s.DescriptionAr, s.Category, s.UpdatedAt))
            .ToListAsync(ct);
    }
}
