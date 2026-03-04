using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("errors.validation.currentPasswordRequired")
            .MaximumLength(256).WithMessage("errors.validation.maxLength");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("errors.validation.newPasswordRequired")
            .MinimumLength(8).WithMessage("errors.validation.passwordMinLength")
            .MaximumLength(256).WithMessage("errors.validation.maxLength")
            .Matches(@"[A-Z]").WithMessage("errors.validation.passwordUppercase")
            .Matches(@"[a-z]").WithMessage("errors.validation.passwordLowercase")
            .Matches(@"[0-9]").WithMessage("errors.validation.passwordDigit")
            .Matches(@"[\W_]").WithMessage("errors.validation.passwordSpecialChar")
            .NotEqual(x => x.CurrentPassword).WithMessage("errors.validation.passwordMustDiffer");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("errors.validation.confirmPasswordRequired")
            .Equal(x => x.NewPassword).WithMessage("errors.validation.passwordsMismatch");
    }
}
