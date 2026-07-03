using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Commands.AskChatbot;

public sealed partial class AskChatbotCommandHandler(
    IApplicationDbContext db,
    IAiService ai,
    AiCostGate gate,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<AskChatbotCommandHandler> logger)
    : IRequestHandler<AskChatbotCommand, ChatAnswerDto>
{
    // ERR-03: conservative PRE-CALL cost reservation for the daily-budget gate.
    // The real per-turn cost is token-based (prompt + replayed history + injected
    // profile context + RAG grounding), so the stub's $0.0003 floor can UNDERCOUNT
    // a real gpt-4o-mini turn several-fold and let a boundary call slip over the
    // cap. Reserve a realistic worst-case (~4k prompt + ~800 completion tokens),
    // still negligible vs the ~$1.00 daily cap. The ACTUAL cost is recorded after
    // the call, so later calls stay exact. (Precise reserve-and-reconcile = v2.)
    private const decimal EstimatedCost = 0.0015m;

    /// <summary>
    /// Max prior turns of the SAME session replayed into the LLM. 20 user-msg +
    /// 20 reply rows cover a long-running session without blowing the prompt
    /// budget — older turns drop off the head (the LLM never sees them).
    /// </summary>
    private const int MaxHistoryTurns = 20;

    public async Task<ChatAnswerDto> Handle(AskChatbotCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        await gate.EnsureWithinDailyBudgetAsync(userId, EstimatedCost, ct).ConfigureAwait(false);

        // PII redaction before we persist or hand to any external provider
        var redacted = RedactPii(request.Message);

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId;

        // Load prior turns of this session — the AI uses them as conversation
        // memory so a follow-up like "what about Stanford?" resolves against
        // the previous "tell me about MIT" turn. Filtered to THIS user so a
        // leaked SessionId can't leak someone else's transcript into the LLM.
        var history = await LoadHistoryAsync(userId, sessionId, ct).ConfigureAwait(false);

        // Inject the student's own profile as a leading system turn so the model
        // can give personalized answers ("are YOU eligible?" against the
        // student's nationality / GPA / field) instead of a generic checklist.
        // Built from real profile data — never fabricated; omitted when empty.
        var profileContext = await BuildProfileContextAsync(userId, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(profileContext))
        {
            history =
            [
                new AiChatHistoryTurn("system", profileContext),
                .. history,
            ];
        }

        var interaction = new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Chatbot,
            Provider = AiProvider.Stub,
            ModelName = "local-router-v1",
            SessionId = sessionId,
            StartedAt = clock.UtcNow,
            PromptText = redacted,
            ResponseText = "",
            CreatedAt = clock.UtcNow,
        };
        db.AiInteractions.Add(interaction);

        try
        {
            var result = await ai
                .AskAsync(userId, sessionId, redacted, history, ct)
                .ConfigureAwait(false);

            var sources = result.Sources
                .Select(s => new ChatSourceDto(s.Title, s.SourceType, s.ScholarshipId, s.Score))
                .ToList();

            interaction.ResponseText = result.Message;
            interaction.PromptTokens = result.PromptTokens;
            interaction.CompletionTokens = result.CompletionTokens;
            interaction.CostUsd = result.EstimatedCostUsd;
            interaction.CompletedAt = clock.UtcNow;
            // Record which knowledge-base documents grounded the answer (RAG audit trail).
            interaction.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { sources });

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new ChatAnswerDto(
                sessionId,
                result.Message,
                result.Disclaimer,
                result.PromptTokens,
                result.CompletionTokens,
                result.EstimatedCostUsd,
                interaction.CompletedAt.Value,
                sources);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            interaction.ErrorMessage = ex.Message;
            interaction.CompletedAt = clock.UtcNow;
            try { await db.SaveChangesAsync(ct).ConfigureAwait(false); }
            catch (Exception saveEx) { logger.LogError(saveEx, "Failed to persist failed chat interaction."); }
            throw;
        }
    }

    /// <summary>
    /// Reads the most recent <see cref="MaxHistoryTurns"/> turns of this session
    /// for this user and flattens them into the canonical user/assistant pairs
    /// the LLM expects. Skips rows with no response (failed turns) so the LLM
    /// doesn't see a dangling user message with no answer.
    /// </summary>
    private async Task<IReadOnlyList<AiChatHistoryTurn>> LoadHistoryAsync(
        Guid userId, string sessionId, CancellationToken ct)
    {
        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId
                        && i.UserId == userId
                        && i.Feature == AiFeature.Chatbot)
            .OrderByDescending(i => i.StartedAt)
            .Take(MaxHistoryTurns)
            .Select(i => new { i.PromptText, i.ResponseText, i.StartedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (rows.Count == 0) return Array.Empty<AiChatHistoryTurn>();

        // Replay in chronological order — the LLM reads top-down, oldest first.
        var turns = new List<AiChatHistoryTurn>(rows.Count * 2);
        foreach (var row in rows.OrderBy(r => r.StartedAt))
        {
            if (!string.IsNullOrWhiteSpace(row.PromptText))
                turns.Add(new AiChatHistoryTurn(AiChatHistoryTurn.UserRole, row.PromptText));
            if (!string.IsNullOrWhiteSpace(row.ResponseText))
                turns.Add(new AiChatHistoryTurn(AiChatHistoryTurn.AssistantRole, row.ResponseText));
        }
        return turns;
    }

    /// <summary>
    /// Builds a compact, factual profile block from the student's real profile
    /// so the LLM can personalize answers. Returns an empty string when there's
    /// nothing useful (e.g. a brand-new profile, or a non-student account), in
    /// which case nothing is injected. Contains no contact PII (no email/phone).
    /// </summary>
    private async Task<string> BuildProfileContextAsync(Guid userId, CancellationToken ct)
    {
        var p = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);
        if (p is null) return string.Empty;

        var facts = new List<string>();
        if (!string.IsNullOrWhiteSpace(p.Nationality)) facts.Add($"Nationality: {p.Nationality}");
        if (p.AcademicLevel is not null) facts.Add($"Academic level: {p.AcademicLevel}");
        if (!string.IsNullOrWhiteSpace(p.FieldOfStudy)) facts.Add($"Field of study: {p.FieldOfStudy}");
        if (!string.IsNullOrWhiteSpace(p.CurrentInstitution)) facts.Add($"Current institution: {p.CurrentInstitution}");
        if (p.Gpa is not null)
            facts.Add($"GPA: {p.Gpa}{(string.IsNullOrWhiteSpace(p.GpaScale) ? string.Empty : $" (scale {p.GpaScale})")}");

        var preferredCountries = ParseStringList(p.PreferredCountriesJson);
        if (preferredCountries.Count > 0) facts.Add($"Preferred study countries: {string.Join(", ", preferredCountries)}");
        var preferredFields = ParseStringList(p.PreferredFieldsJson);
        if (preferredFields.Count > 0) facts.Add($"Preferred fields: {string.Join(", ", preferredFields)}");
        var languages = ParseStringList(p.LanguagesJson);
        if (languages.Count > 0) facts.Add($"Languages: {string.Join(", ", languages)}");

        if (facts.Count == 0) return string.Empty;

        return "STUDENT PROFILE — use these real facts about the user to personalize your "
            + "answer (e.g. judge eligibility against them, tailor recommendations). Do not "
            + "list them back verbatim, and ask for anything that's missing:\n- "
            + string.Join("\n- ", facts);
    }

    private static List<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    internal static string RedactPii(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Order matters: emails first (they anchor on @), then cards (longer
        // digit runs), then phones — phones are the most permissive so they
        // must be last to avoid swallowing card digits.
        var s = EmailRegex().Replace(input, "[redacted-email]");
        s = CreditCardRegex().Replace(s, "[redacted-card]");
        s = PhoneRegex().Replace(s, "[redacted-phone]");
        return s;
    }

    [GeneratedRegex(@"[\w\.-]+@[\w\.-]+\.\w{2,}", RegexOptions.CultureInvariant, 200)]
    private static partial Regex EmailRegex();

    // 10+ digit runs w/ optional separators — covers most international formats
    [GeneratedRegex(@"(?:\+?\d[\d\s\-\.]{8,}\d)", RegexOptions.CultureInvariant, 200)]
    private static partial Regex PhoneRegex();

    // 13-19 digit runs (Visa/MC/Amex/etc.) — block by default
    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.CultureInvariant, 200)]
    private static partial Regex CreditCardRegex();
}
