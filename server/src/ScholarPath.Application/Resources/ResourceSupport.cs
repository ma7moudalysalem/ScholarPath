using System.Text.Json;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources;

/// <summary>Fields a resource must have before it can be submitted or published (PB-009 AC#8).</summary>
public static class ResourcePublishRules
{
    public static IReadOnlyList<string> FindBlockers(Resource r, string? authorBiography)
    {
        var blockers = new List<string>();

        if (string.IsNullOrWhiteSpace(r.TitleEn) || string.IsNullOrWhiteSpace(r.TitleAr))
            blockers.Add("Both English and Arabic titles are required.");

        if (string.IsNullOrWhiteSpace(r.CategorySlug))
            blockers.Add("A category is required.");

        if (r.Type == ResourceType.VideoLink)
        {
            if (string.IsNullOrWhiteSpace(r.ExternalLinkUrl))
                blockers.Add("An external link is required for a video/link resource.");
        }
        else if (string.IsNullOrWhiteSpace(r.ContentMarkdownEn)
                 || string.IsNullOrWhiteSpace(r.ContentMarkdownAr))
        {
            blockers.Add("Both English and Arabic content are required.");
        }

        if (string.IsNullOrWhiteSpace(authorBiography))
            blockers.Add("Add a bio to your profile before publishing.");

        return blockers;
    }
}

/// <summary>Resource tags are persisted as a JSON string array on the entity.</summary>
public static class ResourceTags
{
    public static string Serialize(IEnumerable<string>? tags)
    {
        var clean = (tags ?? [])
            .Select(t => t.Trim())
            .Where(t => t.Length is > 0 and <= 40)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        return JsonSerializer.Serialize(clean);
    }

    public static IReadOnlyList<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary>Resolves the resource author-role label from the caller's roles (PB-009).</summary>
public static class ResourceAuthors
{
    public static string? RoleOf(ICurrentUserService user) =>
        user.IsInRole("Admin") ? "Admin"
        : user.IsInRole("ScholarshipProvider") ? "ScholarshipProvider"
        : user.IsInRole("Consultant") ? "Consultant"
        : null;
}

/// <summary>Maps a materialized Resource entity to its list-item DTO (deserializes tags).</summary>
public static class ResourceMapping
{
    public static ResourceListItemDto ToListItem(Resource r) => new(
        r.Id, r.Slug, r.TitleEn, r.TitleAr,
        r.DescriptionEn, r.DescriptionAr,
        r.Type, r.Status, r.CategorySlug, r.CoverImageUrl, r.AuthorRole,
        ResourceTags.Deserialize(r.TagsJson),
        r.IsFeatured, r.FeaturedOrder, r.PublishedAt);
}
