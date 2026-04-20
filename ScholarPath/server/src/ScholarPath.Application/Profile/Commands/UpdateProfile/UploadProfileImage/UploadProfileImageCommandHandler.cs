using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UploadProfileImage;

public class UploadProfileImageCommandHandler : IRequestHandler<UploadProfileImageCommand, string>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public UploadProfileImageCommandHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(UploadProfileImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        // Save file to local storage (wwwroot/uploads/profiles/)
        var uploadsFolder = Path.Combine("wwwroot", "uploads", "profiles");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await request.FileStream.CopyToAsync(fileStream, cancellationToken);
        }

        var imageUrl = $"/uploads/profiles/{fileName}";

        // Update user profile image URL
        user.ProfileImageUrl = imageUrl;
        await _userManager.UpdateAsync(user);

        return imageUrl;
    }
}
