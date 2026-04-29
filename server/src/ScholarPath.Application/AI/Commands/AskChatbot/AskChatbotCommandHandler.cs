using System.Text.RegularExpressions;
using MediatR;
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
    private const decimal EstimatedCost = 0.0003m;

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
            var result = await ai.AskAsync(userId, sessionId, redacted, ct).ConfigureAwait(false);

            interaction.ResponseText = result.Message;
            interaction.PromptTokens = result.PromptTokens;
            interaction.CompletionTokens = result.CompletionTokens;
            interaction.CostUsd = result.EstimatedCostUsd;
            interaction.CompletedAt = clock.UtcNow;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new ChatAnswerDto(
                sessionId,
                result.Message,
                result.Disclaimer,
                result.PromptTokens,
                result.CompletionTokens,
                result.EstimatedCostUsd,
                interaction.CompletedAt.Value);
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
