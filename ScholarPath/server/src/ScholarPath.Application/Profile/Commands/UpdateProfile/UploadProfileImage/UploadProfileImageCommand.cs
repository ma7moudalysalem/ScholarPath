using MediatR;
using ScholarPath.Application.Common.Attributes;

namespace ScholarPath.Application.Profile.Commands.UploadProfileImage;

[Auditable(AuditAction.ProfileImageUploaded, "User")]
public record UploadProfileImageCommand(
    string FileName,
    string ContentType,
    Stream FileStream) : IRequest<string>;
