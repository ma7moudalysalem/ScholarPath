using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Community.Tags;

/// <summary>
/// Centralized rules for community post tags. Keeps Create/Update commands
/// consistent: same normalization, same limits, same upsert logic.
/// </summary>
public static class TagPolicy
{
    public const int MaxTagsPerPost = 5;
    public const int MaxTagLength = 30;

    private static readonly Regex InvalidChars = new("[^a-z0-9\\-]+", RegexOptions.Compiled);
    private static readonly Regex DashRuns = new("-{2,}", RegexOptions.Compiled);

    /// <summary>
    /// Trim, lowercase, slugify, dedupe and validate the raw tag inputs.
    /// Throws ValidationException if any tag is too long or the collection
    /// exceeds the max-per-post cap.
    /// </summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? raw)
    {
        if (raw is null) return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var tag in raw)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;

            var trimmed = tag.Trim();
            if (trimmed.Length > MaxTagLength)
            {
                throw new FluentValidation.ValidationException(
                    new[] { new FluentValidation.Results.ValidationFailure(
                        "Tags", $"Tag \"{trimmed}\" exceeds {MaxTagLength} characters.") });
            }

            var slug = Slugify(trimmed);
            if (slug.Length == 0) continue;
            if (slug.Length > MaxTagLength) slug = slug[..MaxTagLength];

            if (seen.Add(slug))
            {
                result.Add(slug);
            }
        }

        if (result.Count > MaxTagsPerPost)
        {
            throw new FluentValidation.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Tags", $"At most {MaxTagsPerPost} tags are allowed.") });
        }

        return result;
    }

    /// <summary>
    /// Upserts the given tags and attaches them to the post by replacing the
    /// current set. Existing tag rows are reused via slug lookup; missing ones
    /// are created.
    /// </summary>
    public static async Task AttachTagsAsync(
        IApplicationDbContext db,
        ForumPost post,
        IEnumerable<string>? rawTags,
        CancellationToken ct)
    {
        var slugs = Normalize(rawTags);

        // Always wipe the existing pivot — Create has none, Update needs the
        // replacement semantics.
        if (post.PostTags.Count > 0)
        {
            foreach (var existing in post.PostTags.ToList())
            {
                db.ForumPostTags.Remove(existing);
            }
            post.PostTags.Clear();
        }

        if (slugs.Count == 0) return;

        var existingTags = await db.ForumTags
            .Where(t => slugs.Contains(t.Slug))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bySlug = existingTags.ToDictionary(t => t.Slug, t => t);

        foreach (var slug in slugs)
        {
            if (!bySlug.TryGetValue(slug, out var tag))
            {
                tag = new ForumTag { Name = slug, Slug = slug };
                db.ForumTags.Add(tag);
                bySlug[slug] = tag;
            }
            post.PostTags.Add(new ForumPostTag { ForumPost = post, ForumTag = tag });
        }
    }

    private static string Slugify(string input)
    {
        // CA1308 prefers ToUpperInvariant for normalisation, but URL/tag slugs
        // are conventionally lowercase — the lowercase form is the value users
        // see in the URL bar, not a culture-neutral normalisation step.
#pragma warning disable CA1308
        var lower = input.ToLower(CultureInfo.InvariantCulture);
#pragma warning restore CA1308
        var dashed = InvalidChars.Replace(lower, "-");
        var collapsed = DashRuns.Replace(dashed, "-");
        return collapsed.Trim('-');
    }
}
