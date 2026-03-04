using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.DTOs;

public record CompleteOnboardingRequest(
    UserRole SelectedRole,
    string? CompanyName,
    string? ExpertiseArea,
    string? Bio);
