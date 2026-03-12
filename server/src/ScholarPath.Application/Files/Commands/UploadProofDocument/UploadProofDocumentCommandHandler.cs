using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Files.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Files.Commands.UploadProofDocument;

public class UploadProofDocumentCommandHandler : IRequestHandler<UploadProofDocumentCommand, UploadResponse>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const int MaxFilesPerRequest = 5;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UploadProofDocumentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<UploadResponse> Handle(UploadProofDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        if (request.Files.Count == 0)
        {
            throw new InvalidOperationException("errors.validation.noFilesProvided");
        }

        if (request.Files.Count > MaxFilesPerRequest)
        {
            throw new InvalidOperationException("errors.validation.tooManyFiles");
        }

        foreach (var file in request.Files)
        {
            if (file.Length > MaxFileSize)
            {
                throw new InvalidOperationException("errors.validation.fileTooLarge");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw new InvalidOperationException("errors.validation.invalidFileType");
            }
        }

        if (request.UpgradeRequestId.HasValue)
        {
            var owns = await _dbContext.UpgradeRequests
                .AnyAsync(r => r.Id == request.UpgradeRequestId.Value && r.UserId == userId.Value, cancellationToken);

            if (!owns)
            {
                throw new KeyNotFoundException("errors.admin.upgradeRequestNotFound");
            }
        }

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "upgrade-requests", userId.Value.ToString());
        Directory.CreateDirectory(uploadDir);

        var uploadedFiles = new List<UploadedFileDto>();

        foreach (var file in request.Files)
        {
            var sanitizedName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_")
                .Replace("..", "");
            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{sanitizedName}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDir, uniqueName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.Content.CopyToAsync(stream, cancellationToken);

            var fileEntity = new UpgradeRequestFile
            {
                UpgradeRequestId = request.UpgradeRequestId ?? Guid.Empty,
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            if (request.UpgradeRequestId.HasValue)
            {
                _dbContext.UpgradeRequestFiles.Add(fileEntity);
            }

            uploadedFiles.Add(new UploadedFileDto(
                fileEntity.Id,
                fileEntity.FileName,
                fileEntity.ContentType,
                fileEntity.FileSize,
                $"/uploads/upgrade-requests/{userId.Value}/{uniqueName}"
            ));
        }

        if (request.UpgradeRequestId.HasValue)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UploadResponse(uploadedFiles);
    }
}
