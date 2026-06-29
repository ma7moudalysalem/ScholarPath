using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Azure OpenAI provider with Retrieval-Augmented Generation for the chatbot.
/// Activated by <c>Ai:Provider = AzureOpenAi</c> with an endpoint and key.
///
/// Chat is grounded: the user's message is embedded, the most similar
/// knowledge-base documents are retrieved, and that context is injected into
/// the prompt so answers stay anchored to ScholarPath's real data. If the
/// Azure call fails the service degrades gracefully to the local RAG router.
///
/// Recommendation scoring and eligibility are structured profile-matching
/// tasks, so they delegate to <see cref="LocalAiService"/> — deterministic
/// scoring against the private schema beats LLM guesswork there.
/// </summary>
public sealed class AzureOpenAiService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts,
    IKnowledgeRetriever retriever,
    LocalAiService local,
    IApplicationDbContext db,
    ILogger<AzureOpenAiService> logger) : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    // gpt-4o-mini pricing per token — $0.15 / 1M input, $0.60 / 1M output.
    private const decimal InputCostPerToken = 0.15m / 1_000_000m;
    private const decimal OutputCostPerToken = 0.60m / 1_000_000m;

    public Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId, int topN, CancellationToken ct)
        => local.GenerateRecommendationsAsync(userId, topN, ct);

    public Task<AiEligibilityResult> CheckEligibilityAsync(
        Guid userId, Guid scholarshipId, CancellationToken ct)
        => local.CheckEligibilityAsync(userId, scholarshipId, ct);

    public async Task<AiChatResponse> AskAsync(
        Guid userId,
        string sessionId,
        string message,
        IReadOnlyList<AiChatHistoryTurn> history,
        CancellationToken ct)
    {
        var msg = (message ?? string.Empty).Trim();
        var arabic = RagSupport.IsArabic(msg);
        var ai = opts.Value;

        // If the production App Service is set to `Ai:Provider=AzureOpenAi`
        // but nobody has filled in the endpoint / key, fail fast on the
        // local router instead of letting the Azure call throw — otherwise
        // every user-facing chat request returns 500. Same applies if the
        // retriever's embedding step blows up for the same reason.
        if (!IsAzureChatConfigured(ai))
        {
            logger.LogWarning(
                "Azure OpenAI is selected but Endpoint / ApiKey are not set — answering with the local RAG router.");
            return await local.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }

        // ── Retrieve (the "R" in RAG) ──
        IReadOnlyList<RetrievedDocument> docs;
        try
        {
            docs = await retriever.RetrieveAsync(msg, ai.RagTopK, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Knowledge retrieval failed; answering with the local RAG router instead.");
            return await local.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }

        var relevant = docs.Where(d => d.Score >= ai.RagMinScore).ToList();
        var context = RagSupport.BuildContextBlock(relevant, arabic);

        // ── Augment + generate ──
        try
        {
            var system = arabic ? RagSupport.ChatSystemAr : RagSupport.ChatSystemEn;
            var user = RagSupport.BuildUserPrompt(context, msg, arabic);

            var (text, promptTokens, completionTokens) =
                await ChatCompletionAsync(system, user, history, ct).ConfigureAwait(false);

            var cost = promptTokens * InputCostPerToken + completionTokens * OutputCostPerToken;
            return new AiChatResponse(
                text, Disclaimer, promptTokens, completionTokens, cost,
                RagSupport.ToSources(relevant, arabic));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Azure OpenAI chat failed; falling back to the local RAG router.");
            return await local.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Azure OpenAI chat timed out; falling back to the local RAG router.");
            return await local.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            // Defensive: covers Endpoint/ApiKey races that slip past the
            // upfront check (Options reload, etc.). Don't let it 500.
            logger.LogWarning(ex, "Azure OpenAI chat threw InvalidOperationException; falling back to the local RAG router.");
            return await local.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// True only when both Endpoint and ApiKey are populated. Used to short-
    /// circuit to the local router instead of letting <see cref="ChatCompletionAsync"/>
    /// throw an unhandled <see cref="InvalidOperationException"/>.
    /// </summary>
    private static bool IsAzureChatConfigured(AiOptions ai)
        => !string.IsNullOrWhiteSpace(ai.AzureOpenAi.Endpoint)
        && !string.IsNullOrWhiteSpace(ai.AzureOpenAi.ApiKey);

    // ─── Azure OpenAI HTTP ────────────────────────────────────────────────

    private async Task<(string Text, int PromptTokens, int CompletionTokens)> ChatCompletionAsync(
        string system,
        string user,
        IReadOnlyList<AiChatHistoryTurn> history,
        CancellationToken ct)
    {
        var az = opts.Value.AzureOpenAi;
        if (string.IsNullOrWhiteSpace(az.Endpoint) || string.IsNullOrWhiteSpace(az.ApiKey))
            throw new InvalidOperationException(
                "Ai:AzureOpenAi:Endpoint and ApiKey are required when Ai:Provider=AzureOpenAi.");

        // Deployment priority: DB platform setting → appsettings override → base model.
        var dbDeployment = await PlatformSettingsReader.GetStringAsync(
            db, PlatformSettingsKeys.ActiveFineTunedDeploymentName, null, ct).ConfigureAwait(false);
        var deployment = !string.IsNullOrWhiteSpace(dbDeployment)
            ? dbDeployment
            : !string.IsNullOrWhiteSpace(az.FineTunedDeploymentName)
                ? az.FineTunedDeploymentName
                : az.DeploymentName;

        using var client = httpFactory.CreateClient("azure-openai");
        client.Timeout = TimeSpan.FromSeconds(60);

        var url = $"{az.Endpoint.TrimEnd('/')}/openai/deployments/{deployment}"
                + $"/chat/completions?api-version={az.ApiVersion}";

        // Prepend prior conversation turns between the system prompt and the
        // current user prompt so the LLM has memory of the session — without
        // this every follow-up reads as a brand-new question.
        var messages = new List<object>(history.Count + 2)
        {
            new { role = "system", content = system },
        };
        foreach (var turn in history)
        {
            messages.Add(new { role = turn.Role, content = turn.Content });
        }
        messages.Add(new { role = "user", content = user });

        var body = new
        {
            messages,
            temperature = 0.3,
            max_tokens = 400,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("api-key", az.ApiKey);

        using var resp = await client.SendAsync(request, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content
            .ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct).ConfigureAwait(false)
            ?? throw new HttpRequestException("Azure OpenAI returned an empty payload.");

        var text = payload.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        var promptTokens = payload.Usage?.PromptTokens ?? 0;
        var completionTokens = payload.Usage?.CompletionTokens ?? 0;
        return (text, promptTokens, completionTokens);
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
        [JsonPropertyName("usage")]   public Usage?        Usage   { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class Usage
    {
        [JsonPropertyName("prompt_tokens")]     public int PromptTokens     { get; set; }
        [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    }
}
