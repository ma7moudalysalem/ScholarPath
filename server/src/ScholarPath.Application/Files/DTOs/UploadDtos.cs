namespace ScholarPath.Application.Files.DTOs;

public record UploadedFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    string Path
);

public record UploadResponse(List<UploadedFileDto> Files);
