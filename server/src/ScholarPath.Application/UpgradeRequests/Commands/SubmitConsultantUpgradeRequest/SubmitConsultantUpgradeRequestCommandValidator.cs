using FluentValidation;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgradeRequest;

/// <summary>
/// Mirrors the Consultant rules in <c>SelectRoleCommandValidator</c> so a
/// Student-initiated upgrade is held to the same bar as a fresh Consultant
/// onboarding.
/// </summary>
public sealed class SubmitConsultantUpgradeRequestCommandValidator
    : AbstractValidator<SubmitConsultantUpgradeRequestCommand>
{
    public SubmitConsultantUpgradeRequestCommandValidator()
    {
        RuleFor(x => x.Biography)
            .NotEmpty().MaximumLength(2000)
            .WithMessage("A short bio is required.");
        RuleFor(x => x.ProfessionalTitle)
            .NotEmpty().MaximumLength(150)
            .WithMessage("Professional title is required.");
        RuleFor(x => x.HighestDegree)
            .NotEmpty().MaximumLength(150)
            .WithMessage("Highest degree is required.");
        RuleFor(x => x.FieldOfExpertise)
            .NotEmpty().MaximumLength(200)
            .WithMessage("Field of expertise is required.");
        RuleFor(x => x.YearsOfExperience)
            .NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(80)
            .WithMessage("Years of experience must be 0 or greater.");
        RuleFor(x => x.SessionFeeUsd)
            .NotNull().GreaterThan(0)
            .WithMessage("Session fee must be greater than zero.");
        RuleFor(x => x.SessionDurationMinutes)
            .NotNull()
            .Must(d => d is 30 or 45 or 60 or 90)
            .WithMessage("Session duration must be 30, 45, 60 or 90 minutes.");
        RuleFor(x => x.ExpertiseTags)
            .NotNull()
            .Must(tags => tags is { Length: > 0 })
            .WithMessage("At least one expertise tag is required.");
        RuleFor(x => x.Languages)
            .NotNull()
            .Must(langs => langs is { Length: > 0 })
            .WithMessage("At least one language is required.");
        RuleFor(x => x.Country)
            .NotEmpty().MaximumLength(80)
            .WithMessage("Country is required.");
        RuleFor(x => x.Timezone)
            .NotEmpty().MaximumLength(64)
            .WithMessage("Time zone is required.");
        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(2048)
            .Must(u => string.IsNullOrEmpty(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("LinkedIn URL must be a valid URL.");
        RuleFor(x => x.PortfolioUrl)
            .MaximumLength(2048)
            .Must(u => string.IsNullOrEmpty(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("Portfolio URL must be a valid URL.");
    }
}
