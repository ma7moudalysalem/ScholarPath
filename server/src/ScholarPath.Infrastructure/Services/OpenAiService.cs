using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real OpenAI provider. Activated by setting Ai:Provider = "OpenAi" and
/// providing Ai:OpenAi:ApiKey. Falls back to LocalAiService's scoring logic
/// whenever we need deterministic profile matching (recommendations,
/// eligibility), since the LLM isn't great at scoring against a private
/// schema — we only use OpenAI for the free-form chat turn + a natural-
/// language explanation pass on top of locally-scored items.
///
/// Pricing table is per-million-tokens (gpt-4o-mini today). Update when the
/// model changes.
/// </summary>
public sealed class OpenAiService(
    ApplicationDbContext db,
    IHttpClientFactory httpFactory,
    IOptions<AiOptions> opts,
    ILogger<OpenAiService> logger) : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";
    private const string ChatCompletionsPath = "v1/chat/completions";

    // gpt-4o-mini pricing as of 2026-04 — $0.15 / 1M input, $0.60 / 1M output
    private const decimal InputCostPerToken = 0.15m / 1_000_000m;
    private const decimal OutputCostPerToken = 0.60m / 1_000_000m;

    // Delegate to LocalAiService for the scoring so both providers agree on
    // the fit score math — OpenAI just rephrases the explanation.
    private readonly LocalAiService _local = new(db);

    public async Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId, int topN, CancellationToken ct)
    {
        var local = await _local.GenerateRecommendationsAsync(userId, topN, ct).ConfigureAwait(false);
        if (local.Items.Count == 0) return local;

        // Optional LLM rewrite of each explanation. If the API is unreachable,
        // fall back to the local explanations — the match score + ID stay the
        // same either way, so the UX degrades gracefully.
        try
        {
            var sys = "You rewrite scholarship match explanations into concise (max 140 chars) English and Arabic sentences. Reply ONLY as JSON: {\"en\":\"...\",\"ar\":\"...\"}.";
            var items = new List<AiRecommendationItem>(local.Items.Count);
            var totalPromptTok = local.PromptTokens;
            var totalCompletionTok = local.CompletionTokens;

            foreach (var i in local.Items)
            {
                var user = $"Score: {i.MatchScore}/100\nOriginal EN: {i.ExplanationEn}\nOriginal AR: {i.ExplanationAr}";
                var (text, pTok, cTok) = await ChatCompletionAsync(sys, user, ct).ConfigureAwait(false);
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
            return local;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("OpenAI rewrite timed out; returning local explanations.");
            return local;
        }
    }

    public Task<AiEligibilityResult> CheckEligibilityAsync(Guid userId, Guid scholarshipId, CancellationToken ct)
        // Eligibility is structured — local scoring is strictly better than LLM guesswork.
        => _local.CheckEligibilityAsync(userId, scholarshipId, ct);

    public async Task<AiChatResponse> AskAsync(Guid userId, string sessionId, string message, CancellationToken ct)
    {
        const string sys =
            "You are ScholarPath's help assistant. Stay within: scholarships, applications, eligibility, deadlines, "
            + "consultants, bookings. Keep answers under 600 characters. Never quote personal identifiers back to the user.";

        try
        {
            var (text, pTok, cTok) = await ChatCompletionAsync(sys, message, ct).ConfigureAwait(false);
            var cost = pTok * InputCostPerToken + cTok * OutputCostPerToken;
            return new AiChatResponse(text, Disclaimer, pTok, cTok, cost);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI chat failed; falling back to local router.");
            return await _local.AskAsync(userId, sessionId, message, ct).ConfigureAwait(false);
        }
    }

    // ─── HTTP ─────────────────────────────────────────────────────────────

    private async Task<(string Text, int PromptTokens, int CompletionTokens)> ChatCompletionAsync(
        string system, string user, CancellationToken ct)
    {
        var o = opts.Value.OpenAi;
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            throw new InvalidOperationException("Ai:OpenAi:ApiKey is required when Ai:Provider=OpenAi.");

        using var client = httpFactory.CreateClient("openai");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress ??= new Uri("https://api.openai.com/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey);

        var body = new
        {
            model = o.Model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user",   content = user   },
            },
            temperature = 0.3,
            max_tokens = 256,
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
