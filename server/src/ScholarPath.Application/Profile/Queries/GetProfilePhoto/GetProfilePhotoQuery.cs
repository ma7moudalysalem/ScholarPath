using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Queries.GetProfilePhoto;

/// <summary>
/// Streams a user's stored profile photo (PB-002). Anonymous-accessible — profile
/// photos appear on the public consultant-browse pages.
/// </summary>
public sealed record GetProfilePhotoQuery(Guid UserId) : IRequest<ProfilePhotoResult?>;

/// <summary>
/// Outcome of resolving a profile photo. Exactly one of <see cref="Content"/> /
/// <see cref="RedirectUrl"/> is set: stored uploads carry the bytes, while a
/// photo that is already a public URL (e.g. an SSO provider picture) is returned
/// as a redirect target. A <see langword="null"/> query result means no photo.
/// </summary>
public sealed record ProfilePhotoResult(
    Stream? Content,
    string? ContentType,
    string? RedirectUrl);

public sealed class GetProfilePhotoQueryHandler(
    IApplicationDbContext db,
    IBlobStorageService storage)
    : IRequestHandler<GetProfilePhotoQuery, ProfilePhotoResult?>
{
    public async Task<ProfilePhotoResult?> Handle(GetProfilePhotoQuery request, CancellationToken ct)
    {
        var imageUrl = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.ProfileImageUrl)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

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
            // The stored object is gone — treat it as "no photo".
            return null;
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
}
