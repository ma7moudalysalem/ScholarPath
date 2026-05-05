using MediatR;
using ScholarPath.Application.Common.Attributes;
using ScholarPath.Application.Profile.DTOs;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

[Auditable(AuditAction.ProfileUpdated, "User")]
public record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    string? FieldOfStudy,
    decimal? GPA,
    string? Interests,
    string? Country,
    string? TargetCountry,
    string? Bio,
    DateTime? DateOfBirth) : IRequest<UserProfileDto>;
