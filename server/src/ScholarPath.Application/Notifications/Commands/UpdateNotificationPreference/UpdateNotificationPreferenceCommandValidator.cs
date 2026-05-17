using FluentValidation;

namespace ScholarPath.Application.Notifications.Commands.UpdateNotificationPreference;

public sealed class UpdateNotificationPreferenceCommandValidator
    : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
    }
}
