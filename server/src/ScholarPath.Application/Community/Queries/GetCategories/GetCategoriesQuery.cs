using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Community.DTOs;

namespace ScholarPath.Application.Community.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<List<ForumCategoryDto>>;

public sealed class GetCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoriesQuery, List<ForumCategoryDto>>
{
    public async Task<List<ForumCategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return await db.ForumCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ForumCategoryDto(
                c.Id,
                c.NameEn,
                c.NameAr,
                c.Slug,
                c.DescriptionEn,
                c.DescriptionAr,
                c.DisplayOrder))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
