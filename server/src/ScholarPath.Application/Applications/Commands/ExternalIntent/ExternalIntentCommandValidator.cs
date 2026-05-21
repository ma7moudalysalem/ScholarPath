using System;

using FluentValidation;

namespace ScholarPath.Application.Applications.Commands.ExternalIntent;

public sealed class ExternalIntentCommandValidator : AbstractValidator<ExternalIntentCommand>
{
    public ExternalIntentCommandValidator()
    {
        // Either an in-platform ScholarshipId OR a free-text Title is required.
        // ScholarshipId-less ("purely external") submissions must carry a title.
        RuleFor(v => v.Title)
            .NotEmpty()
            .WithMessage("Scholarship title is required for off-platform external applications.")
            .MaximumLength(300)
            .WithMessage("Scholarship title must not exceed 300 characters.")
            .When(v => !v.ScholarshipId.HasValue || v.ScholarshipId == Guid.Empty);

        RuleFor(v => v.Title)
            .MaximumLength(300)
            .When(v => !string.IsNullOrEmpty(v.Title));

        RuleFor(v => v.Provider)
            .MaximumLength(200)
            .WithMessage("Provider name must not exceed 200 characters.");

        RuleFor(v => v.ExternalTrackingUrl)
            .MaximumLength(2048)
            .WithMessage("Tracking URL must not exceed 2048 characters.")
            .Must(BeAValidHttpUrl)
            .When(v => !string.IsNullOrWhiteSpace(v.ExternalTrackingUrl))
            .WithMessage("Tracking URL must be a valid absolute http(s) URL.");

        RuleFor(v => v.ExternalReferenceId)
            .MaximumLength(200)
            .WithMessage("Reference identifier must not exceed 200 characters.");

        RuleFor(v => v.PersonalNotes)
            .MaximumLength(4000)
            .WithMessage("Notes must not exceed 4000 characters.");
    }

    private static bool BeAValidHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
