using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Application.Scholarships.DTOs
{
    public record ScholarshipDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = default!;
        public string Description { get; init; } = default!;
        public string CategoryName { get; init; } = default!;
        public string? OwnerCompanyName { get; init; }
        public string Status { get; init; } = default!;
        public string FundingType { get; init; } = default!;
        public string TargetLevel { get; init; } = default!;
        public DateTimeOffset Deadline { get; init; }
        public bool IsFeatured { get; init; }
        public string? Slug { get; init; }
        public List<string> FieldsOfStudy { get; init; } = [];

        /// <summary>True when the current user has this scholarship bookmarked.</summary>
        public bool IsBookmarked { get; init; }
    }

    public record ScholarshipDetailDto : ScholarshipDto
    {
        public string? ExternalApplicationUrl { get; init; }
        public string Mode { get; init; } = default!;
        public string? EligibilityRequirements { get; init; }
        public List<ScholarshipChildDto> Children { get; init; } = [];
        public string? ApplicationFormSchemaJson { get; init; }
        public string? RequiredDocumentsJson { get; init; }

        // Raw bilingual + foreign-key fields, shipped alongside the localised
        // Title/Description so the company edit form recovers both languages
        // and the category dropdown comes up pre-selected.
        public string? TitleEn { get; init; }
        public string? TitleAr { get; init; }
        public string? DescriptionEn { get; init; }
        public string? DescriptionAr { get; init; }
        public Guid? CategoryId { get; init; }
    }

    public record ScholarshipChildDto(string ChildType, string Key, string? Value, int SortOrder);
}

