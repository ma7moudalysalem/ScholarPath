using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.DTOs;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    AccountStatus AccountStatus,
    string? ProfileImageUrl,
    bool IsOnboardingComplete)
{
    public bool HasPassword { get; init; }
}
