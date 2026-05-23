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

        /// <summary>
        /// Per-scholarship Review Service Fee (PB-005). Students see this on the
        /// Scholarship Details page before they Apply Now; it is the gross amount
        /// charged for the paid application-support flow. Null when the Company
        /// has not configured a fee yet — in that case the detail page must
        /// disable Apply Now and surface a clear message.
        /// </summary>
        public decimal? ReviewFeeUsd { get; init; }

        /// <summary>
        /// Owner Company user-id, exposed so the frontend can prevent a Company
        /// from "applying" to its own scholarship and so it can address the
        /// payment intent to the right payee.
        /// </summary>
        public Guid? OwnerCompanyId { get; init; }
    }

    public record ScholarshipChildDto(string ChildType, string Key, string? Value, int SortOrder);
}

