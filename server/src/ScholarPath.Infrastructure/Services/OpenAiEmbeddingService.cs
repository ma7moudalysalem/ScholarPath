using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// OpenAI (direct) embedding provider (<c>text-embedding-3-small</c> by default),
/// selected when <c>Ai:Provider = OpenAi</c>. Produces REAL RAG knowledge-base
/// vectors with the same API key the chat already uses — previously an OpenAI
/// deployment had no embedding provider and silently fell back to the local-hash
/// embedder, so retrieval + scholarship matching were semantically meaningless.
///
/// Falls back to <see cref="LocalEmbeddingService"/> when the API key is unset or
/// the call fails, so the KB indexer and chat retriever still produce vectors.
/// </summary>
public sealed class OpenAiEmbeddingService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts,
    LocalEmbeddingService local,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    private const string EmbeddingsUrl = "https://api.openai.com/v1/embeddings";

    public string ModelName =>
        string.IsNullOrWhiteSpace(opts.Value.OpenAi.ApiKey)
            ? local.ModelName
            : $"openai:{opts.Value.OpenAi.EmbeddingModel}";

    public int Dimensions => opts.Value.OpenAi.EmbeddingDimensions;

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

        var o = opts.Value.OpenAi;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
        {
            logger.LogWarning(
                "OpenAI embeddings are selected but Ai:OpenAi:ApiKey is unset — falling back to LocalEmbeddingService.");
            return await local.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
        }

        using var client = httpFactory.CreateClient("openai");
        client.Timeout = TimeSpan.FromSeconds(60);

        // OpenAI rejects empty strings — substitute a single space so indices stay aligned.
        var input = texts
            .Select(t => string.IsNullOrWhiteSpace(t) ? " " : t)
            .ToArray();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EmbeddingsUrl)
            {
                Content = JsonContent.Create(new { model = o.EmbeddingModel, input }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);

            using var resp = await client.SendAsync(request, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var payload = await resp.Content
                .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct).ConfigureAwait(false)
                ?? throw new HttpRequestException("OpenAI returned an empty embeddings payload.");

            // Order by index — the API does not guarantee response order matches the request.
            return payload.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding ?? [])
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex,
                "OpenAI embedding call failed; falling back to LocalEmbeddingService.");
            return await local.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
        }
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")] public List<EmbeddingDatum> Data { get; set; } = [];
    }

    private sealed class EmbeddingDatum
    {
        [JsonPropertyName("index")]     public int      Index     { get; set; }
        [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
    }
}
