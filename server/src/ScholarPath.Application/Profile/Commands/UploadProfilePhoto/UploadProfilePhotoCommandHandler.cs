using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UploadProfilePhoto;

public sealed class UploadProfilePhotoCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService blobStorage)
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
        var url = await blobStorage.UploadAsync(
            request.Content, blobName, request.ContentType, "profile-photos", ct);

        user.ProfileImageUrl = url;
        await db.SaveChangesAsync(ct);
        return url;
    }
}
