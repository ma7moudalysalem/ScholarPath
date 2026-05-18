using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Azure OpenAI embedding provider (<c>text-embedding-3-small</c> by default).
/// POSTs to <c>{Endpoint}/openai/deployments/{embeddingDeployment}/embeddings</c>
/// and is selected when <c>Ai:Provider = AzureOpenAi</c>.
/// </summary>
public sealed class AzureOpenAiEmbeddingService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts) : IEmbeddingService
{
    public string ModelName => $"azure:{opts.Value.AzureOpenAi.EmbeddingDeploymentName}";

    public int Dimensions => opts.Value.AzureOpenAi.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return new float[Dimensions];
        var batch = await EmbedBatchAsync([text], ct).ConfigureAwait(false);
        return batch.Count > 0 ? batch[0] : new float[Dimensions];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (texts.Count == 0) return [];

        var az = opts.Value.AzureOpenAi;
        if (string.IsNullOrWhiteSpace(az.Endpoint) || string.IsNullOrWhiteSpace(az.ApiKey))
            throw new InvalidOperationException(
                "Ai:AzureOpenAi:Endpoint and ApiKey are required when Ai:Provider=AzureOpenAi.");

        using var client = httpFactory.CreateClient("azure-openai");
        client.Timeout = TimeSpan.FromSeconds(60);

        var url = $"{az.Endpoint.TrimEnd('/')}/openai/deployments/{az.EmbeddingDeploymentName}"
                + $"/embeddings?api-version={az.ApiVersion}";

        // Azure rejects empty strings — substitute a single space so indices stay aligned.
        var input = texts
            .Select(t => string.IsNullOrWhiteSpace(t) ? " " : t)
            .ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { input }),
        };
        request.Headers.Add("api-key", az.ApiKey);

        using var resp = await client.SendAsync(request, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content
            .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct).ConfigureAwait(false)
            ?? throw new HttpRequestException("Azure OpenAI returned an empty embeddings payload.");

        // Order by index — the API does not guarantee response order matches the request.
        return payload.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding ?? [])
            .ToList();
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")] public List<EmbeddingDatum> Data { get; set; } = [];
    }

    private sealed class EmbeddingDatum
    {
        [JsonPropertyName("index")]     public int       Index     { get; set; }
        [JsonPropertyName("embedding")] public float[]?  Embedding { get; set; }
    }
}
