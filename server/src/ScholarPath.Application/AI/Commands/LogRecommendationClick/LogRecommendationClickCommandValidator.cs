using FluentValidation;

namespace ScholarPath.Application.Ai.Commands.LogRecommendationClick;

public sealed class LogRecommendationClickCommandValidator : AbstractValidator<LogRecommendationClickCommand>
{
    private static readonly string[] AllowedSources = ["card", "list", "modal"];

    public LogRecommendationClickCommandValidator()
    {
        RuleFor(x => x.ScholarshipId).NotEmpty();
        RuleFor(x => x.Source)
            .MaximumLength(16)
            .Must(s => s is null || AllowedSources.Contains(s))
            .WithMessage("Source must be one of: card, list, modal.");
    }
}
