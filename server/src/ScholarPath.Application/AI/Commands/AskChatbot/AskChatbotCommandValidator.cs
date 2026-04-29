using FluentValidation;

namespace ScholarPath.Application.Ai.Commands.AskChatbot;

public sealed class AskChatbotCommandValidator : AbstractValidator<AskChatbotCommand>
{
    public AskChatbotCommandValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SessionId).MaximumLength(64);
    }
}
