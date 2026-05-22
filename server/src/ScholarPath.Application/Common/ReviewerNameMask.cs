namespace ScholarPath.Application.Common;

/// <summary>
/// Masks a reviewer's name for the "reviews received" surfaces shown to the
/// rated party (a Company or Consultant). Reviews are semi-anonymous: the rated
/// party sees who is broadly behind the feedback without the full identity,
/// which discourages retaliation and keeps the rating channel candid.
///
/// The rule keeps the given name and reduces every following name part to a
/// single initial followed by a dot — e.g. "Sarah Adams" → "Sarah A.",
/// "Mohamed Aly Salem" → "Mohamed A. S.". A blank or whitespace-only name
/// falls back to a neutral placeholder so a card never renders an empty author.
/// </summary>
public static class ReviewerNameMask
{
    private const string Anonymous = "Anonymous";

    /// <summary>
    /// Builds a masked display name from separate first/last name parts (the
    /// shape stored on <c>ApplicationUser</c>).
    /// </summary>
    public static string Mask(string? firstName, string? lastName)
        => Mask($"{firstName} {lastName}");

    /// <summary>
    /// Builds a masked display name from an already-combined full name. Safe for
    /// any input: null, empty, or all-whitespace yields <c>"Anonymous"</c>.
    /// </summary>
    public static string Mask(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Anonymous;
        }

        var parts = fullName.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return Anonymous;
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        var initials = parts
            .Skip(1)
            .Select(p => $"{char.ToUpperInvariant(p[0])}.");

        return $"{parts[0]} {string.Join(' ', initials)}";
    }
}
