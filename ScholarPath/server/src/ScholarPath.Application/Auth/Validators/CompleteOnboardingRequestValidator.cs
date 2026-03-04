using FluentValidation;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Validators;

public class CompleteOnboardingRequestValidator : AbstractValidator<CompleteOnboardingRequest>
{
    public CompleteOnboardingRequestValidator()
    {
        RuleFor(x => x.SelectedRole)
            .Must(role => role == UserRole.Student || role == UserRole.Consultant || role == UserRole.Company)
            .WithMessage("errors.validation.roleNotAllowed");

        // Company-specific: CompanyName is required when role is Company
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("errors.validation.companyNameRequired")
            .MinimumLength(2).WithMessage("errors.validation.companyNameMinLength")
            .MaximumLength(200).WithMessage("errors.validation.companyNameMaxLength")
            .When(x => x.SelectedRole == UserRole.Company);

        // Consultant-specific: ExpertiseArea is required when role is Consultant
        RuleFor(x => x.ExpertiseArea)
            .NotEmpty().WithMessage("errors.validation.expertiseAreaRequired")
            .MaximumLength(500).WithMessage("errors.validation.expertiseAreaMaxLength")
            .When(x => x.SelectedRole == UserRole.Consultant);

        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage("errors.validation.bioRequired")
            .MaximumLength(2000).WithMessage("errors.validation.bioMaxLength")
            .When(x => x.SelectedRole == UserRole.Consultant);
    }
}
