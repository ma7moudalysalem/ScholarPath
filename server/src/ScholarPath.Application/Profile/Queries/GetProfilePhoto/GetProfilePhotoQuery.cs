using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Queries.GetProfilePhoto;

/// <summary>
/// Streams a user's stored profile photo (PB-002). Anonymous-accessible — profile
/// photos appear on the public consultant-browse pages. When the user has no
/// uploaded photo, a deterministic SVG placeholder (initials on a brand-coloured
/// disc) is generated server-side so callers never have to handle a 404.
/// </summary>
public sealed record GetProfilePhotoQuery(Guid UserId) : IRequest<ProfilePhotoResult>;

/// <summary>
/// Outcome of resolving a profile photo. Exactly one of <see cref="Content"/> /
/// <see cref="RedirectUrl"/> is set: stored uploads carry the bytes, while a
/// photo that is already a public URL (e.g. an SSO provider picture) is returned
/// as a redirect target. The handler now always produces a result — if the user
/// has no photo we synthesize an SVG initials avatar.
/// </summary>
public sealed record ProfilePhotoResult(
    Stream? Content,
    string? ContentType,
    string? RedirectUrl);

public sealed class GetProfilePhotoQueryHandler(
    IApplicationDbContext db,
    IBlobStorageService storage)
    : IRequestHandler<GetProfilePhotoQuery, ProfilePhotoResult>
{
    public async Task<ProfilePhotoResult> Handle(GetProfilePhotoQuery request, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.ProfileImageUrl, u.FirstName, u.LastName })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        // Unknown user → still return a neutral avatar so the <img> never errors.
        if (user is null)
            return GenerateInitialsAvatar(request.UserId, firstName: null, lastName: null);

        var imageUrl = user.ProfileImageUrl;

        if (string.IsNullOrWhiteSpace(imageUrl))
            return GenerateInitialsAvatar(request.UserId, user.FirstName, user.LastName);

        // A photo sourced from an SSO provider is already a public absolute URL —
        // the browser can load it directly, so just point it there.
        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new ProfilePhotoResult(null, null, imageUrl);
        }

        // Otherwise it is an opaque `provider:container/key` storage path — stream
        // the bytes out of the (private) blob store.
        try
        {
            var content = await storage.DownloadAsync(imageUrl, ct).ConfigureAwait(false);
            return new ProfilePhotoResult(content, ResolveContentType(imageUrl), null);
        }
        catch (FileNotFoundException)
        {
            // The stored object is gone — fall back to initials instead of a 404
            // so the frontend never sees a broken image.
            return GenerateInitialsAvatar(request.UserId, user.FirstName, user.LastName);
        }
        catch (Exception)
        {
            // Storage transient errors shouldn't surface as broken images on the
            // user-facing pages either. Treat any failure as "no photo".
            return GenerateInitialsAvatar(request.UserId, user.FirstName, user.LastName);
        }
    }

    /// <summary>Infers an image content-type from the stored path's file extension.</summary>
    private static string ResolveContentType(string storagePath)
    {
        var extension = Path.GetExtension(storagePath);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return "image/png";
        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            return "image/webp";
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";
        return "application/octet-stream";
    }

    /// <summary>
    /// Builds a deterministic SVG avatar (initials on a coloured disc) so users
    /// without an uploaded photo still get a friendly visual identity rather
    /// than a 404. The colour is derived from the user id so the same person
    /// always renders the same background everywhere they appear.
    /// </summary>
    private static ProfilePhotoResult GenerateInitialsAvatar(Guid userId, string? firstName, string? lastName)
    {
        var initials = ResolveInitials(firstName, lastName);
        var (background, foreground) = PickPalette(userId);

        // 128×128 viewBox gives a high-res source even for huge avatars.
        // The disc fills the viewBox so the SVG looks correct even when the
        // browser is told not to clip to a circle.
        var svg = new StringBuilder(512)
            .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 128 128\" width=\"128\" height=\"128\">")
            .Append("<defs><linearGradient id=\"g\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"100%\">")
            .Append(CultureInfo.InvariantCulture, $"<stop offset=\"0%\" stop-color=\"{background}\" stop-opacity=\"1\"/>")
            .Append(CultureInfo.InvariantCulture, $"<stop offset=\"100%\" stop-color=\"{DarkerShade(background)}\" stop-opacity=\"1\"/>")
            .Append("</linearGradient></defs>")
            .Append("<rect width=\"128\" height=\"128\" fill=\"url(#g)\"/>")
            .Append("<text x=\"50%\" y=\"50%\" dy=\".35em\" text-anchor=\"middle\" ")
            .Append("font-family=\"-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif\" ")
            .Append(CultureInfo.InvariantCulture, $"font-size=\"56\" font-weight=\"600\" fill=\"{foreground}\">")
            .Append(System.Net.WebUtility.HtmlEncode(initials))
            .Append("</text></svg>")
            .ToString();

        var bytes = Encoding.UTF8.GetBytes(svg);
        return new ProfilePhotoResult(new MemoryStream(bytes), "image/svg+xml", null);
    }

    /// <summary>
    /// Extracts up to 2 letters from the user's name to render as initials.
    /// Falls back to "?" when nothing usable is available (e.g. a deleted user).
    /// </summary>
    private static string ResolveInitials(string? firstName, string? lastName)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();

        if (first.Length > 0 && last.Length > 0)
            return $"{char.ToUpperInvariant(first[0])}{char.ToUpperInvariant(last[0])}";

        var single = first.Length > 0 ? first : last;
        if (single.Length == 0)
            return "?";

        // Use the first character only — works well for both Latin and Arabic.
        var rune = System.Globalization.StringInfo.GetNextTextElement(single, 0);
        return rune.ToUpperInvariant();
    }

    /// <summary>
    /// Stable palette pick — same user id always renders the same colour. The
    /// palette is the brand-tuned set used on the marketing site so avatars
    /// look cohesive next to other UI elements.
    /// </summary>
    private static (string Background, string Foreground) PickPalette(Guid userId)
    {
        // Take the first byte of the user id and modulo into the palette. Stable
        // across processes because the guid bytes don't change.
        var bucket = userId.ToByteArray()[0] % Palette.Length;
        return (Palette[bucket], "#ffffff");
    }

    /// <summary>Brand-tuned colour palette used for initials avatars.</summary>
    private static readonly string[] Palette =
    [
        "#4f46e5", // indigo-600
        "#0891b2", // cyan-600
        "#059669", // emerald-600
        "#d97706", // amber-600
        "#dc2626", // red-600
        "#7c3aed", // violet-600
        "#0284c7", // sky-600
        "#9333ea", // purple-600
        "#ea580c", // orange-600
        "#16a34a", // green-600
        "#2563eb", // blue-600
        "#db2777", // pink-600
    ];

    /// <summary>Picks a darker shade for the gradient stop so the disc has subtle depth.</summary>
    private static string DarkerShade(string hex)
    {
        if (hex.Length != 7 || hex[0] != '#') return hex;
        var r = Convert.ToInt32(hex.AsSpan(1, 2).ToString(), 16);
        var g = Convert.ToInt32(hex.AsSpan(3, 2).ToString(), 16);
        var b = Convert.ToInt32(hex.AsSpan(5, 2).ToString(), 16);
        r = (int)(r * 0.75);
        g = (int)(g * 0.75);
        b = (int)(b * 0.75);
        return string.Create(CultureInfo.InvariantCulture, $"#{r:x2}{g:x2}{b:x2}");
    }
}
