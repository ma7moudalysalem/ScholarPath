using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ScholarPath. Application.Common.Models;


namespace ScholarPath.Application.Scholarships.Queries
{
    public record GetScholarshipByIdQuery (Guid Id ,string?Language="en") : IRequest<ScholarshipDetailDto>;

    public class GetScholarshipByIdQueryHandler(IApplicationDbContext db)
        : IRequestHandler<GetScholarshipByIdQuery, ScholarshipDetailDto>
    {
        public async Task<ScholarshipDetailDto> Handle(GetScholarshipByIdQuery request, CancellationToken ct)
        {
            var lang = request.Language ?? "en";

            var entity = await db.Scholarships
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Children)
                .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

            if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);


            if (entity.Status != ScholarshipStatus.Open)
            {
                var errorMessage = lang == "ar" ? "المنحة غير متاحة حاليا " : "Scholarship is not current available";
                throw new ConflictException(errorMessage);
            }
            
            return new ScholarshipDetailDto
            {
                Id = entity.Id,
                Title = lang == "ar" ? (entity.TitleAr ?? entity.TitleEn):(entity.TitleEn ?? entity.TitleAr),
                Mode = entity.Mode.ToString(),
                ExternalApplicationUrl = entity.ExternalApplicationUrl,
                EligibilityRequirements = lang == "ar" ? entity.EligibilityRequirementsAr : entity.EligibilityRequirementsEn,
                ApplicationFormSchemaJson = entity.ApplicationFormSchemaJson,
                RequiredDocumentsJson = entity.RequiredDocumentsJson,
                Children = entity.Children.OrderBy(c => c.SortOrder).Select(c => new ScholarshipChildDto(
                    c.ChildType,
                    lang == "ar" ? c.KeyAr ?? c.KeyEn : c.KeyEn,
                    lang == "ar" ? c.ValueAr ?? c.ValueEn : c.ValueEn,
                    c.SortOrder
                )).ToList() ?? new List<ScholarshipChildDto>()
            };
        }
    }
}

