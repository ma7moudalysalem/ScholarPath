using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Azure OpenAI embedding provider (<c>text-embedding-3-small</c> by default).
/// POSTs to <c>{Endpoint}/openai/deployments/{embeddingDeployment}/embeddings</c>
/// and is selected when <c>Ai:Provider = AzureOpenAi</c>.
///
/// Falls back to <see cref="LocalEmbeddingService"/> when:
///   - the Azure endpoint or API key is unset (early-out, no HTTP)
///   - the embedding deployment isn't provisioned (404 from Azure)
///   - any other transient HTTP failure
/// so the KB indexer and chat retriever still produce vectors when the
/// Azure resource only has a chat deployment configured.
/// </summary>
public sealed class AzureOpenAiEmbeddingService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts,
    LocalEmbeddingService local,
    ILogger<AzureOpenAiEmbeddingService> logger) : IEmbeddingService
{
    public string ModelName =>
        IsAzureEmbeddingConfigured()
            ? $"azure:{opts.Value.AzureOpenAi.EmbeddingDeploymentName}"
            : local.ModelName;

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

        // If Azure isn't configured at all, don't even try — drop straight
        // to the local embedder so the KB rebuild still produces vectors.
        if (!IsAzureEmbeddingConfigured())
        {
            logger.LogWarning(
                "Azure OpenAI embeddings are selected but not configured — falling back to LocalEmbeddingService.");
            return await local.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
        }

        var az = opts.Value.AzureOpenAi;
        using var client = httpFactory.CreateClient("azure-openai");
        client.Timeout = TimeSpan.FromSeconds(60);

        var url = $"{az.Endpoint!.TrimEnd('/')}/openai/deployments/{az.EmbeddingDeploymentName}"
                + $"/embeddings?api-version={az.ApiVersion}";

        // Azure rejects empty strings — substitute a single space so indices stay aligned.
        var input = texts
            .Select(t => string.IsNullOrWhiteSpace(t) ? " " : t)
            .ToArray();

        try
        {
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       && !ct.IsCancellationRequested)
        {
            // 404 (deployment not provisioned), 401/403 (bad key), network — log
            // the cause and fall through to the deterministic local embedder so
            // the KB rebuild and chat retrieval still work.
            logger.LogWarning(ex,
                "Azure OpenAI embedding call failed (deployment '{Deployment}'); falling back to LocalEmbeddingService.",
                az.EmbeddingDeploymentName);
            return await local.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// True only when Endpoint, ApiKey, and the embedding deployment name are
    /// all populated. Used to short-circuit straight to the local embedder
    /// when the operator hasn't provisioned an embedding model in Azure.
    /// </summary>
    private bool IsAzureEmbeddingConfigured()
    {
        var az = opts.Value.AzureOpenAi;
        return !string.IsNullOrWhiteSpace(az.Endpoint)
            && !string.IsNullOrWhiteSpace(az.ApiKey)
            && !string.IsNullOrWhiteSpace(az.EmbeddingDeploymentName);
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
