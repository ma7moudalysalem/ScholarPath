namespace ScholarPath.Application.Profile.DTOs;

public record UserProfileDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? ProfileImageUrl,
    string? FieldOfStudy,
    decimal? GPA,
    string? Interests,
    string? Country,
    string? TargetCountry,
    string? Bio,
    DateTime? DateOfBirth,
    int ProfileCompleteness);
