using MediatR;

namespace ScholarPath.Application.Profile.Commands.UploadProfileImage;

public record UploadProfileImageCommand(
    string FileName,
    string ContentType,
    Stream FileStream) : IRequest<string>;
