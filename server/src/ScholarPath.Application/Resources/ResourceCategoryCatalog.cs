namespace ScholarPath.Application.Resources;

/// <summary>
/// Canonical list of <see cref="Domain.Entities.Resource.CategorySlug"/>
/// values the platform recognises. Kept as a closed catalog so the client
/// dropdown, the validator, and the seed data never drift apart — adding a
/// new category is a single-line edit here.
/// </summary>
public static class ResourceCategoryCatalog
{
    /// <summary>
    /// All recognised slugs. Order is the order they appear in the picker
    /// dropdown. Keep the slug lower-kebab-case (matches the seed convention
    /// in <c>DbSeeder.Resources</c>) so URLs and filters stay consistent.
    /// </summary>
    public static readonly IReadOnlyList<string> Slugs = new[]
    {
        "applications",
        "essays",
        "interviews",
        "language",
        "finance",
        "visas",
        "planning",
        "life-abroad",
        "misc",
    };

    /// <summary>True when <paramref name="slug"/> is one of the recognised
    /// categories. Empty / null is allowed for callers that treat the field
    /// as optional (a Draft resource doesn't have to pick one yet).</summary>
    public static bool IsKnown(string? slug) =>
        string.IsNullOrEmpty(slug) || Slugs.Contains(slug);
}
