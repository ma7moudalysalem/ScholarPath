using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("errors.validation.firstNameRequired")
            .MinimumLength(2).WithMessage("errors.validation.firstNameMinLength")
            .MaximumLength(50).WithMessage("errors.validation.firstNameMaxLength")
            .Matches(@"^[\p{L}\s\-'.]+$").WithMessage("errors.validation.invalidNameCharacters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("errors.validation.lastNameRequired")
            .MinimumLength(2).WithMessage("errors.validation.lastNameMinLength")
            .MaximumLength(50).WithMessage("errors.validation.lastNameMaxLength")
            .Matches(@"^[\p{L}\s\-'.]+$").WithMessage("errors.validation.invalidNameCharacters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("errors.validation.emailRequired")
            .EmailAddress().WithMessage("errors.validation.emailInvalid")
            .MaximumLength(256).WithMessage("errors.validation.emailMaxLength");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("errors.validation.passwordRequired")
            .MinimumLength(8).WithMessage("errors.validation.passwordMinLength")
            .MaximumLength(256).WithMessage("errors.validation.maxLength")
            .Matches(@"[A-Z]").WithMessage("errors.validation.passwordUppercase")
            .Matches(@"[a-z]").WithMessage("errors.validation.passwordLowercase")
            .Matches(@"[0-9]").WithMessage("errors.validation.passwordDigit")
            .Matches(@"[\W_]").WithMessage("errors.validation.passwordSpecialChar");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("errors.validation.confirmPasswordRequired")
            .Equal(x => x.Password).WithMessage("errors.validation.passwordsMismatch");
    }
}
