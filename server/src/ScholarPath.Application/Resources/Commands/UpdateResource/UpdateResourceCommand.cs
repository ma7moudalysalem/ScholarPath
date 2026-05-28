using FluentValidation;
using Ganss.Xss;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Commands.UpdateResource;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Edits a <c>Draft</c> resource. Allowed for the author or an admin (PB-009).</summary>
[Auditable(AuditAction.Update, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Updated resource {ResourceId}")]
public sealed record UpdateResourceCommand(
    Guid ResourceId,
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
    IReadOnlyList<ResourceChapterInput>? Chapters) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DescriptionEn).MaximumLength(2000);
        RuleFor(x => x.DescriptionAr).MaximumLength(2000);
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

public sealed class UpdateResourceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<UpdateResourceCommandHandler> logger)
    : IRequestHandler<UpdateResourceCommand, bool>
{
    public async Task<bool> Handle(UpdateResourceCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var resource = await db.Resources
            .Include(r => r.Chapters)
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        var isAdmin = currentUser.IsInRole("Admin");
        if (resource.AuthorUserId != userId && !isAdmin)
            throw new ForbiddenAccessException("You can only edit your own resources.");

        // A resource is only editable while still a draft — once submitted or
        // published, edits would bypass the review workflow.
        if (resource.Status != ResourceStatus.Draft)
            throw new ConflictException(
                "Only a draft resource can be edited. Reject it back to draft first.");

        var sanitizer = new HtmlSanitizer();

        resource.TitleEn = sanitizer.Sanitize(request.TitleEn);
        resource.TitleAr = sanitizer.Sanitize(request.TitleAr);
        resource.DescriptionEn = Clean(sanitizer, request.DescriptionEn);
        resource.DescriptionAr = Clean(sanitizer, request.DescriptionAr);
        resource.ContentMarkdownEn = Clean(sanitizer, request.ContentMarkdownEn);
        resource.ContentMarkdownAr = Clean(sanitizer, request.ContentMarkdownAr);
        resource.ExternalLinkUrl = request.ExternalLinkUrl;
        resource.CoverImageUrl = request.CoverImageUrl;
        resource.Type = request.Type;
        resource.CategorySlug = request.CategorySlug;
        resource.TagsJson = ResourceTags.Serialize(request.Tags);

        // Replace the chapter set wholesale — simplest correct sync for the editor.
        if (resource.Chapters.Count > 0)
            db.ResourceChapters.RemoveRange(resource.Chapters);

        if (request.Chapters is { Count: > 0 })
        {
            var order = 0;
            foreach (var ch in request.Chapters.OrderBy(c => c.SortOrder))
            {
                db.ResourceChapters.Add(new ResourceChild
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

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Resource {ResourceId} updated by {UserId}.", resource.Id, userId);
        return true;
    }

    private static string? Clean(HtmlSanitizer s, string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : s.Sanitize(value);
}
