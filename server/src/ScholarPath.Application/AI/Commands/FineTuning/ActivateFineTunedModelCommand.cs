using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.FineTuning;

/// <summary>
/// Stores a fine-tuned deployment name in <c>PlatformSettings</c> so the chatbot
/// immediately starts using it — no App Service restart or config change required.
/// </summary>
[Auditable(AuditAction.ConfigChanged, "AiModel",
    SummaryTemplate = "Activated fine-tuned chat model {DeploymentName}")]
public sealed record ActivateFineTunedModelCommand(string DeploymentName) : IRequest<ActivateFineTunedModelResult>;

public sealed record ActivateFineTunedModelResult(string DeploymentName, bool Replaced);

/// <summary>
/// Clears the active fine-tuned deployment and reverts the chatbot to the base model.
/// </summary>
[Auditable(AuditAction.ConfigChanged, "AiModel",
    SummaryTemplate = "Reverted the chatbot to the base model")]
public sealed record DeactivateFineTunedModelCommand : IRequest;

public sealed class ActivateFineTunedModelCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ActivateFineTunedModelCommand, ActivateFineTunedModelResult>
{
    public async Task<ActivateFineTunedModelResult> Handle(
        ActivateFineTunedModelCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DeploymentName))
            throw new ConflictException("Deployment name must not be empty.");

        var previous = await PlatformSettingsReader.GetStringAsync(
            db, PlatformSettingsKeys.ActiveFineTunedDeploymentName, null, ct).ConfigureAwait(false);

        await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.ActiveFineTunedDeploymentName, request.DeploymentName.Trim(), ct)
            .ConfigureAwait(false);

        return new ActivateFineTunedModelResult(
            DeploymentName: request.DeploymentName.Trim(),
            Replaced: !string.IsNullOrWhiteSpace(previous));
    }
}

public sealed class DeactivateFineTunedModelCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeactivateFineTunedModelCommand>
{
    public async Task Handle(DeactivateFineTunedModelCommand request, CancellationToken ct)
        => await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.ActiveFineTunedDeploymentName, string.Empty, ct)
            .ConfigureAwait(false);
}
