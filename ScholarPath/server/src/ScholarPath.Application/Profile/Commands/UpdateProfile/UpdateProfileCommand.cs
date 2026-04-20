using MediatR;
using ScholarPath.Application.Profile.DTOs;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

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
    