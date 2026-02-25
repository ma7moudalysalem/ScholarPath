using FluentValidation;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Validators;

public class CompleteOnboardingRequestValidator : AbstractValidator<CompleteOnboardingRequest>
{
    public CompleteOnboardingRequestValidator()
    {
        RuleFor(x => x.SelectedRole)
            .IsInEnum().WithMessage("Invalid role selected.")
            .Must(role => role != UserRole.Admin)
            .WithMessage("Admin role cannot be selected during onboarding.");

        RuleFor(x => x.SelectedRole)
            .Must(role => role == UserRole.Student || role == UserRole.Consultant || role == UserRole.Company)
            .WithMessage("Selected role must be Student, Consultant, or Company.");

        When(x => x.SelectedRole == UserRole.Company, () =>
        {
            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Company name is required for Company accounts.")
                .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");
        });

        When(x => x.SelectedRole == UserRole.Consultant, () =>
        {
            RuleFor(x => x.ExpertiseArea)
                .NotEmpty().WithMessage("Expertise area is required for Consultant accounts.")
                .MaximumLength(500).WithMessage("Expertise area must not exceed 500 characters.");

            RuleFor(x => x.Bio)
                .NotEmpty().WithMessage("Bio is required for Consultant accounts.")
                .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.");
        });
    }
}
