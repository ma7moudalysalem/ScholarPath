using System.Text.Json;
using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ScholarPath.Application.Scholarships.Queries
{
    public record GetScholarshipByIdQuery(Guid Id, string Language = "en") : IRequest<ScholarshipDetailDto>;

    public class GetScholarshipByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<GetScholarshipByIdQuery, ScholarshipDetailDto>
    {
        public async Task<ScholarshipDetailDto> Handle(GetScholarshipByIdQuery request, CancellationToken ct)
        {
            var lang = request.Language;

            var entity = await db.Scholarships
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Children)
                .Include(s => s.OwnerScholarshipProvider)
                .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

            if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);

            var userId = currentUser.UserId;
            var isBookmarked = userId != null && await db.SavedScholarships
                .AnyAsync(sv => sv.ScholarshipId == entity.Id && sv.UserId == userId, ct);

            return new ScholarshipDetailDto
            {
                Id = entity.Id,
                Title = lang == "ar" ? (entity.TitleAr ?? entity.TitleEn) : (entity.TitleEn ?? entity.TitleAr),
                Description = lang == "ar"
                    ? (entity.DescriptionAr ?? entity.DescriptionEn)
                    : (entity.DescriptionEn ?? entity.DescriptionAr),
                CategoryName = lang == "ar"
                    ? (entity.Category?.NameAr ?? entity.Category?.NameEn ?? "")
                    : (entity.Category?.NameEn ?? entity.Category?.NameAr ?? ""),
                OwnerScholarshipProviderName = entity.OwnerScholarshipProvider != null
                    ? entity.OwnerScholarshipProvider.FirstName + " " + entity.OwnerScholarshipProvider.LastName
                    : "Global Provider",
                Status = entity.Status.ToString(),
                FundingType = entity.FundingType.ToString(),
                TargetLevel = entity.TargetLevel.ToString(),
                IsFeatured = entity.IsFeatured,
                Slug = entity.Slug,
                IsBookmarked = isBookmarked,
                Deadline = entity.Deadline,
                Mode = entity.Mode.ToString(),
                Country = ParseFirstCountry(entity.TargetCountriesJson),
                ExternalApplicationUrl = entity.ExternalApplicationUrl,
                EligibilityRequirements = lang == "ar"
                    ? (entity.EligibilityRequirementsAr ?? entity.EligibilityRequirementsEn)
                    : (entity.EligibilityRequirementsEn ?? entity.EligibilityRequirementsAr),
                ApplicationFormSchemaJson = entity.ApplicationFormSchemaJson,
                RequiredDocumentsJson = entity.RequiredDocumentsJson,

                FieldsOfStudy = entity.FieldsOfStudyJson is not null
                    ? JsonSerializer.Deserialize<List<string>>(entity.FieldsOfStudyJson) ?? []
                    : [],

                // Raw bilingual + categoryId for the company edit form (PB-005).
                TitleEn = entity.TitleEn,
                TitleAr = entity.TitleAr,
                DescriptionEn = entity.DescriptionEn,
                DescriptionAr = entity.DescriptionAr,
                CategoryId = entity.CategoryId,

                // Review Service Fee surfaced for the Student Apply Now flow.
                ReviewFeeUsd = entity.ReviewFeeUsd,
                OwnerScholarshipProviderId = entity.OwnerScholarshipProviderId,

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

        // TargetCountriesJson stores a JSON array; the listing form captures a
        // single country. Return the first element for display, tolerating a
        // legacy plain-string value or malformed JSON.
        private static string? ParseFirstCountry(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list is { Count: > 0 } ? list[0] : null;
            }
            catch (JsonException)
            {
                return json; // legacy non-JSON value
            }
        }
    }
}
