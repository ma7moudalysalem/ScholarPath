using FluentValidation;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Fields.FirstName).MaximumLength(100)
            .When(x => x.Fields.FirstName is not null);
        RuleFor(x => x.Fields.LastName).MaximumLength(100)
            .When(x => x.Fields.LastName is not null);
        RuleFor(x => x.Fields.Biography).MaximumLength(2000)
            .When(x => x.Fields.Biography is not null);
        RuleFor(x => x.Fields.Gpa).InclusiveBetween(0m, 5m)
            .When(x => x.Fields.Gpa.HasValue);
        RuleFor(x => x.Fields.SessionFeeUsd).GreaterThanOrEqualTo(0m)
            .When(x => x.Fields.SessionFeeUsd.HasValue);
    }
}
