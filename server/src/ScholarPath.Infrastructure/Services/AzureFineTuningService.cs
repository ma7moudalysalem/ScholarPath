using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Wraps the Azure OpenAI Files and Fine-Tuning Jobs REST API so Application
/// layer commands remain HTTP-agnostic.
/// </summary>
public sealed class AzureFineTuningService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts) : IAzureFineTuningService
{
    private AzureOpenAiOptions Az => opts.Value.AzureOpenAi;

    private string BaseUrl => Az.Endpoint?.TrimEnd('/') ?? throw new InvalidOperationException(
        "Ai:AzureOpenAi:Endpoint is required for fine-tuning. Configure it in App Service settings.");

    private string ApiKey => Az.ApiKey ?? throw new InvalidOperationException(
        "Ai:AzureOpenAi:ApiKey is required for fine-tuning. Configure it in App Service settings.");

    private string ApiVersion => Az.ApiVersion;

    public async Task<string> UploadTrainingFileAsync(string jsonlContent, CancellationToken ct)
    {
        using var client = CreateClient();

        var uploadUri = new Uri($"{BaseUrl}/openai/files?api-version={ApiVersion}");
        var fileBytes = Encoding.UTF8.GetBytes(jsonlContent);

        using var form = new MultipartFormDataContent();

        // ByteArrayContent lifetime is managed by the parent MultipartFormDataContent.
#pragma warning disable CA2000
        var fileContent = new ByteArrayContent(fileBytes);
#pragma warning restore CA2000
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(fileContent, "file", "scholarpath-finetune.jsonl");

        using var purposeContent = new StringContent("fine-tune");
        form.Add(purposeContent, "purpose");

        using var resp = await client.PostAsync(uploadUri, form, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(resp, "upload training file").ConfigureAwait(false);

        var result = await resp.Content.ReadFromJsonAsync<FileUploadResponse>(cancellationToken: ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from Azure OpenAI Files API.");

        // Poll until the file finishes processing (usually a few seconds).
        await WaitForFileProcessedAsync(client, result.Id, ct).ConfigureAwait(false);
        return result.Id;
    }

    public async Task<string> CreateFineTuningJobAsync(
        string fileId, string baseModel, CancellationToken ct)
    {
        using var client = CreateClient();

        var jobUri = new Uri($"{BaseUrl}/openai/fine_tuning/jobs?api-version={ApiVersion}");

        var body = new { training_file = fileId, model = baseModel };
        using var resp = await client.PostAsJsonAsync(jobUri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(resp, "create fine-tuning job").ConfigureAwait(false);

        var result = await resp.Content.ReadFromJsonAsync<FineTuningJobResponse>(cancellationToken: ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from Azure OpenAI fine-tuning API.");

        return result.Id;
    }

    public async Task<FineTuningJobStatus> GetJobStatusAsync(string jobId, CancellationToken ct)
    {
        using var client = CreateClient();

        var statusUri = new Uri($"{BaseUrl}/openai/fine_tuning/jobs/{jobId}?api-version={ApiVersion}");
        using var resp = await client.GetAsync(statusUri, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(resp, "get fine-tuning job status").ConfigureAwait(false);

        var result = await resp.Content.ReadFromJsonAsync<FineTuningJobResponse>(cancellationToken: ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from Azure OpenAI fine-tuning API.");

        string? errorMessage = null;
        if (result.Error is not null)
            errorMessage = $"{result.Error.Code}: {result.Error.Message}";

        return new FineTuningJobStatus(
            JobId: result.Id,
            Status: result.Status,
            FineTunedModel: result.FineTunedModel,
            Error: errorMessage);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private HttpClient CreateClient()
    {
        var client = httpFactory.CreateClient("azure-openai");
        client.Timeout = TimeSpan.FromSeconds(120);
        client.DefaultRequestHeaders.Add("api-key", ApiKey);
        return client;
    }

    private async Task WaitForFileProcessedAsync(HttpClient client, string fileId, CancellationToken ct)
    {
        var pollUri = new Uri($"{BaseUrl}/openai/files/{fileId}?api-version={ApiVersion}");
        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(2000, ct).ConfigureAwait(false);
            using var resp = await client.GetAsync(pollUri, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) break;

            var file = await resp.Content.ReadFromJsonAsync<FileStatusResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
            if (file?.Status == "processed") return;
        }
        // If polling times out we still return — the fine-tuning job itself validates the file.
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, string operation)
    {
        if (resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new InvalidOperationException(
            $"Azure OpenAI {operation} failed ({(int)resp.StatusCode}): {body}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Response models (Azure OpenAI REST shape — not exposed outside this file)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class FileUploadResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    }

    private sealed class FileStatusResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    }

    private sealed class FineTuningJobResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
        [JsonPropertyName("fine_tuned_model")] public string? FineTunedModel { get; set; }
        [JsonPropertyName("error")] public FineTuningError? Error { get; set; }
    }

    private sealed class FineTuningError
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}
