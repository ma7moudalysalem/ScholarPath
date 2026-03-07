using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/files")]
[Authorize]
public class FilesController : BaseController
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const int MaxFilesPerRequest = 5;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public FilesController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _env = env;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(30_000_000)] // 30 MB total limit
    public async Task<IActionResult> Upload(
        [FromForm] List<IFormFile> files,
        [FromForm] Guid? upgradeRequestId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return UnauthorizedResult("errors.auth.userNotFound");

        if (files.Count == 0)
            return BadRequestResult("errors.validation.noFilesProvided");

        if (files.Count > MaxFilesPerRequest)
            return BadRequestResult("errors.validation.tooManyFiles");

        foreach (var file in files)
        {
            if (file.Length > MaxFileSize)
                return BadRequestResult("errors.validation.fileTooLarge");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return BadRequestResult("errors.validation.invalidFileType");
        }

        // Verify upgrade request ownership if specified
        if (upgradeRequestId.HasValue)
        {
            var owns = await _dbContext.UpgradeRequests
                .AnyAsync(r => r.Id == upgradeRequestId.Value && r.UserId == user.Id, cancellationToken);
            if (!owns) return NotFoundResult("errors.admin.upgradeRequestNotFound");
        }

        var uploadDir = Path.Combine(_env.ContentRootPath, "uploads", "upgrade-requests", user.Id.ToString());
        Directory.CreateDirectory(uploadDir);

        var uploadedFiles = new List<object>();

        foreach (var file in files)
        {
            var sanitizedName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_")
                .Replace("..", "");
            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{sanitizedName}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDir, uniqueName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            var fileEntity = new UpgradeRequestFile
            {
                UpgradeRequestId = upgradeRequestId ?? Guid.Empty,
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            if (upgradeRequestId.HasValue)
            {
                _dbContext.UpgradeRequestFiles.Add(fileEntity);
            }

            uploadedFiles.Add(new
            {
                fileEntity.Id,
                fileEntity.FileName,
                fileEntity.ContentType,
                fileEntity.FileSize,
                Path = $"/uploads/upgrade-requests/{user.Id}/{uniqueName}"
            });
        }

        if (upgradeRequestId.HasValue)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { Files = uploadedFiles });
    }
}
