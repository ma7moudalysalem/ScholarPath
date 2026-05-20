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

        var now = DateTimeOffset.UtcNow;

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
            Status = ScholarshipStatus.Open,
            OwnerCompanyId = ownerCompanyId,
            // Slug is REQUIRED + UNIQUE on the schema — generate from the
            // English title with a short Guid suffix so two scholarships sharing
            // the same name (very common) never collide on insert.
            Slug = GenerateSlug(request.TitleEn, request.TitleAr),
            // The scholarship goes live immediately (Status=Open), so stamp the
            // open timestamp now — listing-sort / freshness-since-open reads this.
            OpenedAt = now,
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
