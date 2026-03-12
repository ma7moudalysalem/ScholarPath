using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.CompleteOnboarding;

public record CompleteOnboardingCommand(
    UserRole SelectedRole,
    string? CompanyName,
    string? ExpertiseArea,
    string? Bio) : IRequest<UserDto>;
