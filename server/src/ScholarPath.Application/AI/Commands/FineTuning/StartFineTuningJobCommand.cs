using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Ai.Queries.ExportFineTuningDataset;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Ai.Commands.FineTuning;

/// <summary>
/// Admin command — exports the JSONL training dataset, uploads it to Azure OpenAI
/// Files API, and starts a supervised fine-tuning job. Returns the Azure job ID
/// immediately; the job runs asynchronously and its status can be polled via
/// <see cref="ScholarPath.Application.Ai.Queries.FineTuning.GetFineTuningJobStatusQuery"/>.
/// </summary>
public sealed record StartFineTuningJobCommand(
    string BaseModel = "gpt-4o-mini") : IRequest<StartFineTuningJobResult>;

public sealed record StartFineTuningJobResult(
    string JobId,
    string FileId,
    string BaseModel,
    int TrainingExamples);

public sealed class StartFineTuningJobCommandHandler(
    IApplicationDbContext db,
    IAzureFineTuningService fineTuning,
    IMediator mediator) : IRequestHandler<StartFineTuningJobCommand, StartFineTuningJobResult>
{
    private static readonly HashSet<string> TerminalStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "succeeded", "failed", "cancelled", "canceled" };

    public async Task<StartFineTuningJobResult> Handle(
        StartFineTuningJobCommand request, CancellationToken ct)
    {
        // 0. BUG-06: reject a duplicate submission while a prior job is still in
        //    flight. Only ONE job id is tracked (FineTuningLastJobId); starting a
        //    new job would overwrite it and orphan the running one. Poll the LIVE
        //    status (not the stale stored string) so a legitimate retry after a
        //    real failure/cancel is still allowed.
        var lastJobId = await PlatformSettingsReader.GetStringAsync(
            db, PlatformSettingsKeys.FineTuningLastJobId, null, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(lastJobId))
        {
            string? liveStatus = null;
            try
            {
                var live = await fineTuning.GetJobStatusAsync(lastJobId, ct).ConfigureAwait(false);
                liveStatus = live.Status;
            }
            catch
            {
                // A stale/expired job id may 404 on Azure — never let a bad stored
                // id permanently block new runs; treat an unresolvable job as done.
                liveStatus = null;
            }

            if (!string.IsNullOrWhiteSpace(liveStatus) && !TerminalStatuses.Contains(liveStatus))
            {
                throw new ConflictException(
                    $"A fine-tuning job ({lastJobId}) is already in progress (status: {liveStatus}). " +
                    "Wait for it to finish or cancel it before starting a new one.");
            }
        }

        // 1. Generate the JSONL dataset from existing platform data.
        var dataset = await mediator
            .Send(new ExportFineTuningDatasetQuery(), ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(dataset.Jsonl) || dataset.ExampleCount == 0)
            throw new ConflictException(
                "The fine-tuning dataset is empty. Add open scholarships or FAQ entries first.");

        // 2. Upload the JSONL file to Azure OpenAI.
        var fileId = await fineTuning
            .UploadTrainingFileAsync(dataset.Jsonl, ct)
            .ConfigureAwait(false);

        // 3. Start the fine-tuning job.
        var jobId = await fineTuning
            .CreateFineTuningJobAsync(fileId, request.BaseModel, ct)
            .ConfigureAwait(false);

        // 4. Persist the job metadata in PlatformSettings so the admin can
        //    poll status and activate the model without passing the ID manually.
        await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.FineTuningLastJobId, jobId, ct).ConfigureAwait(false);

        await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.FineTuningLastJobStatus, "submitted", ct).ConfigureAwait(false);

        // Clear any previously finished model info so the UI shows fresh state.
        await PlatformSettingsReader.SetAsync(
            db, PlatformSettingsKeys.FineTuningLastFinishedModel, string.Empty, ct).ConfigureAwait(false);

        return new StartFineTuningJobResult(jobId, fileId, request.BaseModel, dataset.ExampleCount);
    }
}
