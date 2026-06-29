using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Ai.Queries.FineTuning;

/// <summary>
/// Returns the current status of the most recently started fine-tuning job.
/// Polls Azure if a job ID is stored in <c>PlatformSettings</c>.
/// </summary>
public sealed record GetFineTuningJobStatusQuery : IRequest<FineTuningStatusDto>;

public sealed record FineTuningStatusDto(
    string? JobId,
    string Status,
    string? FineTunedModel,
    string? Error,
    string? ActiveDeploymentName,
    bool HasActiveDeployment);

public sealed class GetFineTuningJobStatusQueryHandler(
    IApplicationDbContext db,
    IAzureFineTuningService fineTuning) : IRequestHandler<GetFineTuningJobStatusQuery, FineTuningStatusDto>
{
    public async Task<FineTuningStatusDto> Handle(
        GetFineTuningJobStatusQuery request, CancellationToken ct)
    {
        var jobId = await PlatformSettingsReader.GetStringAsync(
            db, PlatformSettingsKeys.FineTuningLastJobId, null, ct).ConfigureAwait(false);

        var activeDeployment = await PlatformSettingsReader.GetStringAsync(
            db, PlatformSettingsKeys.ActiveFineTunedDeploymentName, null, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return new FineTuningStatusDto(
                JobId: null,
                Status: "none",
                FineTunedModel: null,
                Error: null,
                ActiveDeploymentName: activeDeployment,
                HasActiveDeployment: !string.IsNullOrWhiteSpace(activeDeployment));
        }

        // Poll Azure for the live status.
        var azureStatus = await fineTuning.GetJobStatusAsync(jobId, ct).ConfigureAwait(false);

        // Persist latest status and finished-model name so offline callers see the
        // last-known state without hitting Azure on every page load.
        await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.FineTuningLastJobStatus, azureStatus.Status, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(azureStatus.FineTunedModel))
        {
            await PlatformSettingsReader.SetAsync(
                db, PlatformSettingsKeys.FineTuningLastFinishedModel, azureStatus.FineTunedModel, ct)
                .ConfigureAwait(false);
        }

        return new FineTuningStatusDto(
            JobId: jobId,
            Status: azureStatus.Status,
            FineTunedModel: azureStatus.FineTunedModel,
            Error: azureStatus.Error,
            ActiveDeploymentName: activeDeployment,
            HasActiveDeployment: !string.IsNullOrWhiteSpace(activeDeployment));
    }
}
