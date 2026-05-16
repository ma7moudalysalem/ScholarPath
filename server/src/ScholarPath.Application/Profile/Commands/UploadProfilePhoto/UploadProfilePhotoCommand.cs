using MediatR;

namespace ScholarPath.Application.Profile.Commands.UploadProfilePhoto;

/// <summary>Uploads a profile photo and returns the stored blob URL.</summary>
public sealed record UploadProfilePhotoCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IRequest<string>;
