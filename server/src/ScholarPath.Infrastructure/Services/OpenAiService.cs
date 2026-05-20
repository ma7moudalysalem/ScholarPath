using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real OpenAI provider. Activated by setting <c>Ai:Provider = OpenAi</c> and
/// providing <c>Ai:OpenAi:ApiKey</c>.
///
/// Chat is Retrieval-Augmented: the message is embedded, the most similar
/// knowledge-base documents are retrieved, and that context is injected into
/// the prompt. Recommendation scoring and eligibility delegate to
/// <see cref="LocalAiService"/> — the LLM only rewrites recommendation
/// explanations, since it is not good at scoring against a private schema.
///
/// Pricing constants are per-token for gpt-4o-mini; update when the model changes.
/// </summary>
public sealed class OpenAiService(
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts,
    IKnowledgeRetriever retriever,
    LocalAiService localAi,
    ILogger<OpenAiService> logger) : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";
    private const string ChatCompletionsPath = "v1/chat/completions";

    // gpt-4o-mini pricing as of 2026-04 — $0.15 / 1M input, $0.60 / 1M output.
    private const decimal InputCostPerToken = 0.15m / 1_000_000m;
    private const decimal OutputCostPerToken = 0.60m / 1_000_000m;

    public async Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId, int topN, CancellationToken ct)
    {
        var scored = await localAi.GenerateRecommendationsAsync(userId, topN, ct).ConfigureAwait(false);
        if (scored.Items.Count == 0) return scored;

        // Optional LLM rewrite of each explanation. If the API is unreachable,
        // fall back to the local explanations — the match score + ID stay the
        // same either way, so the UX degrades gracefully.
        try
        {
            const string sys = "You rewrite scholarship match explanations into concise (max 140 chars) "
                + "English and Arabic sentences. Reply ONLY as JSON: {\"en\":\"...\",\"ar\":\"...\"}.";
            var items = new List<AiRecommendationItem>(scored.Items.Count);
            var totalPromptTok = scored.PromptTokens;
            var totalCompletionTok = scored.CompletionTokens;

            foreach (var i in scored.Items)
            {
                var user = $"Score: {i.MatchScore}/100\nOriginal EN: {i.ExplanationEn}\nOriginal AR: {i.ExplanationAr}";
                // Recommendation rewriting is a one-shot request — no prior
                // turns to replay, so pass an empty history.
                var (text, pTok, cTok) = await ChatCompletionAsync(
                    sys, user, Array.Empty<AiChatHistoryTurn>(), ct).ConfigureAwait(false);
                totalPromptTok += pTok;
                totalCompletionTok += cTok;

                var (en, ar) = TryParseBilingualJson(text, i.ExplanationEn, i.ExplanationAr);
                items.Add(new AiRecommendationItem(i.ScholarshipId, i.MatchScore, en, ar));
            }

            return new AiRecommendationResult(items, Disclaimer, totalPromptTok, totalCompletionTok);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI rewrite failed; returning local explanations.");
            return scored;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("OpenAI rewrite timed out; returning local explanations.");
            return scored;
        }
    }

    public Task<AiEligibilityResult> CheckEligibilityAsync(Guid userId, Guid scholarshipId, CancellationToken ct)
        // Eligibility is structured — local scoring is strictly better than LLM guesswork.
        => localAi.CheckEligibilityAsync(userId, scholarshipId, ct);

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

        // ── Retrieve (the "R" in RAG) ──
        var docs = await retriever.RetrieveAsync(msg, ai.RagTopK, ct).ConfigureAwait(false);
        var relevant = docs.Where(d => d.Score >= ai.RagMinScore).ToList();
        var context = RagSupport.BuildContextBlock(relevant, arabic);

        // ── Augment + generate ──
        try
        {
            var system = arabic ? RagSupport.ChatSystemAr : RagSupport.ChatSystemEn;
            var user = RagSupport.BuildUserPrompt(context, msg, arabic);

            var (text, pTok, cTok) = await ChatCompletionAsync(system, user, history, ct).ConfigureAwait(false);
            var cost = pTok * InputCostPerToken + cTok * OutputCostPerToken;
            return new AiChatResponse(
                text, Disclaimer, pTok, cTok, cost,
                RagSupport.ToSources(relevant, arabic));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI chat failed; falling back to the local RAG router.");
            return await localAi.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("OpenAI chat timed out; falling back to the local RAG router.");
            return await localAi.AskAsync(userId, sessionId, msg, history, ct).ConfigureAwait(false);
        }
    }

    // ─── HTTP ─────────────────────────────────────────────────────────────

    private async Task<(string Text, int PromptTokens, int CompletionTokens)> ChatCompletionAsync(
        string system,
        string user,
        IReadOnlyList<AiChatHistoryTurn> history,
        CancellationToken ct)
    {
        var o = opts.Value.OpenAi;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("Ai:OpenAi:ApiKey is required when Ai:Provider=OpenAi.");

        using var client = httpFactory.CreateClient("openai");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress ??= new Uri("https://api.openai.com/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);

        // Prepend prior turns so the LLM has conversation memory across the
        // session (a follow-up like "what about the deadline?" needs the
        // previous "tell me about X" turn to resolve "the").
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
            model = o.Model,
            messages,
            temperature = 0.3,
            max_tokens = 400,
        };

        using var resp = await client.PostAsJsonAsync(ChatCompletionsPath, body, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct).ConfigureAwait(false)
            ?? throw new HttpRequestException("OpenAI returned an empty payload.");

        var text = payload.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        var pTok = payload.Usage?.PromptTokens ?? 0;
        var cTok = payload.Usage?.CompletionTokens ?? 0;
        return (text, pTok, cTok);
    }

    private static (string En, string Ar) TryParseBilingualJson(string raw, string fallbackEn, string fallbackAr)
    {
        try
        {
            // Model might wrap in ```json fences — strip.
            var trimmed = raw;
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNl = trimmed.IndexOf('\n', StringComparison.Ordinal);
                var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNl > 0 && lastFence > firstNl)
                    trimmed = trimmed.Substring(firstNl + 1, lastFence - firstNl - 1);
            }

            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            var en = root.TryGetProperty("en", out var e) ? e.GetString() ?? fallbackEn : fallbackEn;
            var ar = root.TryGetProperty("ar", out var a) ? a.GetString() ?? fallbackAr : fallbackAr;
            return (en, ar);
        }
        catch (JsonException)
        {
            return (fallbackEn, fallbackAr);
        }
    }

    // ─── OpenAI response shape (subset we consume) ────────────────────────

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
