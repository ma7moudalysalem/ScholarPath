namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Thin wrapper around Azure OpenAI's Files + Fine-Tuning Job REST APIs.
/// Implementations live in Infrastructure; the interface keeps Application
/// decoupled from the HTTP details.
/// </summary>
public interface IAzureFineTuningService
{
    /// <summary>
    /// Uploads <paramref name="jsonlContent"/> to the Azure OpenAI Files API
    /// and waits until the file is processed. Returns the file ID.
    /// Throws <see cref="InvalidOperationException"/> when Azure OpenAI is not configured.
    /// </summary>
    Task<string> UploadTrainingFileAsync(string jsonlContent, CancellationToken ct);

    /// <summary>
    /// Creates a fine-tuning job against <paramref name="baseModel"/> using the
    /// pre-uploaded <paramref name="fileId"/>. Returns the job ID immediately —
    /// the job runs asynchronously on Azure (30–90 minutes).
    /// </summary>
    Task<string> CreateFineTuningJobAsync(string fileId, string baseModel, CancellationToken ct);

    /// <summary>
    /// Polls the status of an existing fine-tuning job. Returns the current
    /// <see cref="FineTuningJobStatus"/> and, once succeeded, the trained
    /// model name.
    /// </summary>
    Task<FineTuningJobStatus> GetJobStatusAsync(string jobId, CancellationToken ct);
}

/// <summary>
/// Snapshot of a fine-tuning job as returned by Azure OpenAI.
/// </summary>
public sealed record FineTuningJobStatus(
    string JobId,
    string Status,
    string? FineTunedModel,
    string? Error);
