using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.SendBroadcast;

public sealed class SendBroadcastCommandValidator : AbstractValidator<SendBroadcastCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "Company", "Consultant", "Admin", "SuperAdmin", "Moderator",
    };

    public SendBroadcastCommandValidator()
    {
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(160);
        RuleFor(x => x.BodyEn).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.BodyAr).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TargetRole)
            .Must(r => r is null || AllowedRoles.Contains(r))
            .WithMessage("TargetRole must be null or one of the platform roles.");
    }
}
