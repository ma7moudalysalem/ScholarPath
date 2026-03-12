using MediatR;
using Microsoft.AspNetCore.Http;
using ScholarPath.Application.Files.DTOs;

namespace ScholarPath.Application.Files.Commands.UploadProofDocument;

public record FileDto(string FileName, string ContentType, long Length, Stream Content);

public record UploadProofDocumentCommand(
    List<FileDto> Files,
    Guid? UpgradeRequestId) : IRequest<UploadResponse>;
