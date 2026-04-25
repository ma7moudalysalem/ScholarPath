using FluentValidation;

namespace ScholarPath.Application.Payments.Commands.HandleStripeWebhook;

public sealed class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty();
    }
}
