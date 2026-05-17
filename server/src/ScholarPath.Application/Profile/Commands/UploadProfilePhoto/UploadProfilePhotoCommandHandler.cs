using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UploadProfilePhoto;

public sealed class UploadProfilePhotoCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService blobStorage,
    IFileScanService fileScan,
    ILogger<UploadProfilePhotoCommandHandler> logger)
    : IRequestHandler<UploadProfilePhotoCommand, string>
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<string> Handle(UploadProfilePhotoCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        if (request.Length is <= 0 or > MaxBytes)
            throw new ConflictException("Profile photo must be between 1 byte and 5 MB.");

        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException("Profile photo must be a .jpg, .png or .webp image.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var blobName = $"{userId:N}{extension}";

        // Antivirus scan BEFORE storing the image (security NFR). Fail-closed:
        // reject an infected file and reject one that could not be scanned.
        var scan = await fileScan.ScanAsync(request.Content, blobName, ct);
        if (scan.Verdict == FileScanVerdict.Infected)
        {
            logger.LogWarning(
                "Profile photo upload by {UserId} rejected — malware detected: {Detail}",
                userId, scan.Detail);
            throw new ConflictException($"File rejected — malware detected: {request.FileName}");
        }
        if (scan.Verdict == FileScanVerdict.ScanUnavailable)
        {
            logger.LogError(
                "Profile photo upload by {UserId} rejected — could not be virus-scanned: {Detail}",
                userId, scan.Detail);
            throw new ConflictException(
                "File could not be virus-scanned; upload rejected. Try again later.");
        }

        var url = await blobStorage.UploadAsync(
            request.Content, blobName, request.ContentType, "profile-photos", ct);

        user.ProfileImageUrl = url;
        await db.SaveChangesAsync(ct);
        return url;
    }
}
