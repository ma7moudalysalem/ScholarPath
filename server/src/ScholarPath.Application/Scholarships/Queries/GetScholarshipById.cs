using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ScholarPath.Application.Scholarships.Queries
{
    public record GetScholarshipByIdQuery(Guid Id, string Language = "en") : IRequest<ScholarshipDetailDto>;

    public class GetScholarshipByIdQueryHandler(IApplicationDbContext db)
        : IRequestHandler<GetScholarshipByIdQuery, ScholarshipDetailDto>
    {
        public async Task<ScholarshipDetailDto> Handle(GetScholarshipByIdQuery request, CancellationToken ct)
        {
            var lang = request.Language;

            var entity = await db.Scholarships
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Children)
                .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

            if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);

            // I5: Removed 409 throw for closed scholarships - client will handle UI state

            return new ScholarshipDetailDto
            {
                Id = entity.Id,
                Title = lang == "ar" ? (entity.TitleAr ?? entity.TitleEn) : (entity.TitleEn ?? entity.TitleAr),
                Description = lang == "ar" ? entity.DescriptionAr : entity.DescriptionEn,
                CategoryName = lang == "ar" ? entity.Category?.NameAr ?? "" : entity.Category?.NameEn ?? "",
                Status = entity.Status.ToString(),
                Deadline = entity.Deadline,
                Mode = entity.Mode.ToString(),
                ExternalApplicationUrl = entity.ExternalApplicationUrl,
                EligibilityRequirements = lang == "ar" ? entity.EligibilityRequirementsAr : entity.EligibilityRequirementsEn,
                ApplicationFormSchemaJson = entity.ApplicationFormSchemaJson,
                RequiredDocumentsJson = entity.RequiredDocumentsJson,
                // I6: ToList() clean up
                Children = entity.Children
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new ScholarshipChildDto(
                        c.ChildType,
                        lang == "ar" ? c.KeyAr ?? c.KeyEn : c.KeyEn,
                        lang == "ar" ? c.ValueAr ?? c.ValueEn : c.ValueEn,
                        c.SortOrder
                    )).ToList()
            };
        }
    }
}
