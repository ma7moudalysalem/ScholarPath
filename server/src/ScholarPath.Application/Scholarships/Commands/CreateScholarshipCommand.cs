using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using FluentValidation;
using ScholarPath.Domain.Interfaces;
namespace ScholarPath.Application.Scholarships.Commands;

public record CreateScholarshipCommand : IRequest<Guid>
{
    public string TitleEn { get; init; } = null!;
    public string TitleAr { get; init; } = null!;
    public string DescriptionEn { get; init; } = null!;
    public string DescriptionAr { get; init; } = null!;
    public Guid CategoryId { get; init; }
    public DateTimeOffset Deadline { get; init; }
    public FundingType FundingType { get; init; }
    public AcademicLevel TargetLevel { get; init; }
    /// <summary>Optional list of eligible academic fields of study.</summary>
    public string[]? FieldsOfStudy { get; init; }

    /// <summary>
    /// Listing mode — defaults to <see cref="ListingMode.InApp"/>. Companies
    /// today only post in-app listings; admin-driven flows may extend this in
    /// future. Switching to <see cref="ListingMode.ExternalUrl"/> requires a
    /// well-formed absolute <see cref="ExternalApplicationUrl"/>.
    /// </summary>
    public ListingMode Mode { get; init; } = ListingMode.InApp;

    /// <summary>
    /// Apply-out URL for <see cref="ListingMode.ExternalUrl"/> listings.
    /// Required (and must be an absolute http/https URL) when Mode is
    /// ExternalUrl; ignored otherwise.
    /// </summary>
    public string? ExternalApplicationUrl { get; init; }

    /// <summary>
    /// Per-scholarship Review Service Fee in USD (PB-005). Required for in-app
    /// listings — drives the gross amount the Student authorises when they
    /// click Apply Now and creates the paid CompanyReview support flow.
    /// </summary>
    public decimal? ReviewFeeUsd { get; init; }
}

public class CreateScholarshipCommandValidator : AbstractValidator<CreateScholarshipCommand>
{
    public CreateScholarshipCommandValidator()
    {
        RuleFor(v => v.TitleEn).MaximumLength(300).NotEmpty();
        RuleFor(v => v.TitleAr).MaximumLength(300).NotEmpty();
        RuleFor(v => v.DescriptionEn).NotEmpty().MaximumLength(4000);
        // DescriptionAr was previously unvalidated, but the schema marks the
        // column as NOT NULL — an empty Arabic description used to slip through
        // and fail at SaveChanges with a generic 500 instead of a clean 400.
        RuleFor(v => v.DescriptionAr).NotEmpty().MaximumLength(4000);
        RuleFor(v => v.CategoryId).NotEmpty();

        // Spec: deadline must be at least 7 days out. Evaluated per-request
        // (a lambda) — NOT captured once at validator construction.
        RuleFor(v => v.Deadline)
            .Must(deadline => deadline > DateTimeOffset.UtcNow.AddDays(7))
            .WithMessage("Deadline must be at least 7 days from now.");

        // PB-005: in-app listings must declare a Review Service Fee at create
        // time so the Apply Now flow always has a price to authorise. External
        // listings don't need one — the Student leaves the platform to apply,
        // and the company is paid out-of-band.
        When(v => v.Mode == ListingMode.InApp, () =>
        {
            RuleFor(v => v.ReviewFeeUsd)
                .NotNull()
                .WithMessage("Review Service Fee is required.")
                .GreaterThan(0m)
                .WithMessage("Review Service Fee must be greater than 0.")
                .LessThanOrEqualTo(500m)
                .WithMessage("Review Service Fee cannot exceed $500.");
        });

        // External-URL listings need a real apply target — the column is
        // nullable up to 2048 chars on the schema, so without this check a
        // Mode=ExternalUrl row could end up with no link at all and the apply
        // button on the detail page would dead-end.
        When(v => v.Mode == ListingMode.ExternalUrl, () =>
        {
            RuleFor(v => v.ExternalApplicationUrl)
                .NotEmpty()
                .WithMessage("ExternalApplicationUrl is required when Mode is ExternalUrl.")
                .MaximumLength(2048)
                .Must(BeAbsoluteHttpUrl)
                .WithMessage("ExternalApplicationUrl must be a valid absolute http or https URL.");
        });
    }

    private static bool BeAbsoluteHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}

public class CreateScholarshipCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<CreateScholarshipCommand, Guid>
{
    public async Task<Guid> Handle(CreateScholarshipCommand request, CancellationToken ct)
    {
        // Only an authenticated Company account can publish an in-app listing.
        if (!user.IsInRole("Company"))
            throw new ForbiddenAccessException("Only a Company account can create scholarship listings.");

        var ownerCompanyId = user.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var entity = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = request.TitleEn,
            TitleAr = request.TitleAr,
            DescriptionEn = request.DescriptionEn,
            DescriptionAr = request.DescriptionAr,
            CategoryId = request.CategoryId,
            Deadline = request.Deadline,
            FundingType = request.FundingType,
            TargetLevel = request.TargetLevel,
            FieldsOfStudyJson = request.FieldsOfStudy is { Length: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(request.FieldsOfStudy)
                : null,
            Mode = request.Mode,
            ExternalApplicationUrl = request.Mode == ListingMode.ExternalUrl
                ? request.ExternalApplicationUrl
                : null,
            // Only persist the fee for in-app listings; external listings settle
            // off-platform so the column stays null and the Apply Now button
            // becomes a redirect rather than a paid flow.
            ReviewFeeUsd = request.Mode == ListingMode.InApp
                ? request.ReviewFeeUsd
                : null,
            // Company-created listings always start in the admin moderation
            // queue (FR-SCH-10). They become Open only after an admin Approve,
            // which is the path that stamps OpenedAt. We deliberately leave
            // OpenedAt null here so "freshness-since-open" sorts and analytics
            // measure from the approve moment, not from the create moment.
            Status = ScholarshipStatus.UnderReview,
            OwnerCompanyId = ownerCompanyId,
            // Slug is REQUIRED + UNIQUE on the schema — generate from the
            // English title with a short Guid suffix so two scholarships sharing
            // the same name (very common) never collide on insert.
            Slug = GenerateSlug(request.TitleEn, request.TitleAr),
        };

        db.Scholarships.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    /// <summary>
    /// Builds a URL-safe slug from the listing title — drops non-alphanumeric
    /// characters, lower-cases the rest, then suffixes a short Guid so the
    /// unique index never blocks the insert. Mirrors the pattern used by
    /// <see cref="ScholarPath.Application.Resources.Commands.CreateResource.CreateResourceCommandHandler"/>
    /// so both content types feel the same in URLs.
    /// </summary>
    private static string GenerateSlug(string titleEn, string titleAr)
    {
        // Prefer the English title when present; fall back to the Arabic one so
        // a Company that left TitleEn blank wouldn't have the request 500 here
        // (the validator already rejects both-empty, but be defensive).
        var source = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : titleAr;
        if (string.IsNullOrWhiteSpace(source)) source = "scholarship";

        // CA1308 prefers ToUpperInvariant for normalisation, but URL slugs are
        // by convention lowercase — and we only lowercase to fold ASCII A-Z, never
        // to compare or de-dupe across locales. Suppress the warning here.
#pragma warning disable CA1308
        var basis = new string(source.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
#pragma warning restore CA1308
        basis = string.Join('-', basis.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (basis.Length == 0) basis = "scholarship";
        if (basis.Length > 280) basis = basis[..280];
        return $"{basis}-{Guid.NewGuid():N}"[..(basis.Length + 9)];
    }
}
