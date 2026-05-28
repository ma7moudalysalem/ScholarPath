using FluentValidation;
using Ganss.Xss;
using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Commands.CreateResource;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Creates a resource as a <c>Draft</c> (PB-009). Author = the authenticated caller.</summary>
[Auditable(AuditAction.Create, "Resource", SummaryTemplate = "Created resource {TitleEn}")]
public sealed record CreateResourceCommand(
    string TitleEn,
    string TitleAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string? ContentMarkdownEn,
    string? ContentMarkdownAr,
    string? ExternalLinkUrl,
    string? CoverImageUrl,
    ResourceType Type,
    string? CategorySlug,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<ResourceChapterInput>? Chapters) : IRequest<Guid>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DescriptionEn).MaximumLength(2000);
        RuleFor(x => x.DescriptionAr).MaximumLength(2000);
        // Closed-set check against the canonical catalog so the dropdown,
        // validator, and seed cannot drift apart. NULL / empty stays allowed
        // for the Draft state — the publish guard (ResourcePublishRules)
        // separately requires the field to be filled before Submit.
        RuleFor(x => x.CategorySlug)
            .MaximumLength(120)
            .Must(ResourceCategoryCatalog.IsKnown)
            .WithMessage("Unknown resource category.");
        RuleFor(x => x.ExternalLinkUrl).MaximumLength(2048);
        RuleFor(x => x.CoverImageUrl).MaximumLength(2048);
        RuleFor(x => x.Type).IsInEnum();
        RuleForEach(x => x.Chapters).ChildRules(c =>
        {
            c.RuleFor(ch => ch.TitleEn).NotEmpty().MaximumLength(300);
            c.RuleFor(ch => ch.TitleAr).NotEmpty().MaximumLength(300);
        });
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class CreateResourceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<CreateResourceCommandHandler> logger)
    : IRequestHandler<CreateResourceCommand, Guid>
{
    public async Task<Guid> Handle(CreateResourceCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Only consultants, companies, and admins author resources (PB-009).
        var authorRole = ResourceAuthors.RoleOf(currentUser)
            ?? throw new ForbiddenAccessException(
                "Only consultants, companies, and admins can author resources.");

        var sanitizer = new HtmlSanitizer();

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            TitleEn = sanitizer.Sanitize(request.TitleEn),
            TitleAr = sanitizer.Sanitize(request.TitleAr),
            Slug = Slugify(request.TitleEn),
            DescriptionEn = Clean(sanitizer, request.DescriptionEn),
            DescriptionAr = Clean(sanitizer, request.DescriptionAr),
            ContentMarkdownEn = Clean(sanitizer, request.ContentMarkdownEn),
            ContentMarkdownAr = Clean(sanitizer, request.ContentMarkdownAr),
            ExternalLinkUrl = request.ExternalLinkUrl,
            CoverImageUrl = request.CoverImageUrl,
            AuthorUserId = userId,
            AuthorRole = authorRole,
            Type = request.Type,
            Status = ResourceStatus.Draft,
            CategorySlug = request.CategorySlug,
            TagsJson = ResourceTags.Serialize(request.Tags),
        };

        if (request.Chapters is { Count: > 0 })
        {
            var order = 0;
            foreach (var ch in request.Chapters.OrderBy(c => c.SortOrder))
            {
                resource.Chapters.Add(new ResourceChild
                {
                    Id = Guid.NewGuid(),
                    ResourceId = resource.Id,
                    TitleEn = sanitizer.Sanitize(ch.TitleEn),
                    TitleAr = sanitizer.Sanitize(ch.TitleAr),
                    ContentMarkdownEn = Clean(sanitizer, ch.ContentMarkdownEn),
                    ContentMarkdownAr = Clean(sanitizer, ch.ContentMarkdownAr),
                    SortOrder = order++,
                    EstimatedReadMinutes = Math.Clamp(ch.EstimatedReadMinutes, 0, 600),
                });
            }
        }

        db.Resources.Add(resource);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Resource {ResourceId} created by {UserId} ({Role}).", resource.Id, userId, authorRole);
        return resource.Id;
    }

    private static string? Clean(HtmlSanitizer s, string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : s.Sanitize(value);

    private static string Slugify(string title)
    {
        var basis = new string(title.Trim().ToLower()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        basis = string.Join('-', basis.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (basis.Length == 0) basis = "resource";
        if (basis.Length > 280) basis = basis[..280];
        return $"{basis}-{Guid.NewGuid():N}"[..(basis.Length + 9)];
    }
}
