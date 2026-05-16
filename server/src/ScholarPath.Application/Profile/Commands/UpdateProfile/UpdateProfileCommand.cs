using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

[Auditable(AuditAction.Update, "UserProfile", TargetIdProperty = nameof(UserProfileDto.UserId))]
public sealed record UpdateProfileCommand(UpdateProfileRequestDto Fields) : IRequest<UserProfileDto>;
