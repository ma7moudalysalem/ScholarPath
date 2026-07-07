using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Queries.GetAiSessionTurns;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>One persisted AI chat turn — user prompt + assistant response.</summary>
public sealed record AiSessionTurnDto(
    Guid Id,
    string PromptText,
    string ResponseText,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? PromptTokens,
    int? CompletionTokens,
    decimal? CostUsd,
    // RAG citations that grounded the assistant reply — re-hydrated from
    // AiInteraction.MetadataJson so they survive a session reload (they were
    // previously only returned live on the ChatAnswerDto and lost on refetch).
    IReadOnlyList<ChatSourceDto> Sources);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns every turn of a given AI chat session, oldest-first, scoped to the
/// authenticated user — a session id leaked elsewhere cannot read someone
/// else's chat transcript. Used by the chatbot UI when the user clicks a past
/// session from the sidebar to resume it.
/// </summary>
public sealed record GetAiSessionTurnsQuery(string SessionId)
    : IRequest<IReadOnlyList<AiSessionTurnDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetAiSessionTurnsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAiSessionTurnsQuery, IReadOnlyList<AiSessionTurnDto>>
{
    private static readonly JsonSerializerOptions SourcesJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<AiSessionTurnDto>> Handle(
        GetAiSessionTurnsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Array.Empty<AiSessionTurnDto>();
        }

        // Fetch the raw rows (including MetadataJson) then map in memory — the
        // sources live as JSON inside MetadataJson and can't be deserialized in
        // an EF/SQL projection.
        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.SessionId == request.SessionId
                        && i.UserId == userId
                        && i.Feature == AiFeature.Chatbot)
            .OrderBy(i => i.StartedAt)
            .Select(i => new
            {
                i.Id,
                i.PromptText,
                i.ResponseText,
                i.StartedAt,
                i.CompletedAt,
                i.PromptTokens,
                i.CompletionTokens,
                i.CostUsd,
                i.MetadataJson,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .Select(i => new AiSessionTurnDto(
                i.Id,
                i.PromptText,
                i.ResponseText,
                i.StartedAt,
                i.CompletedAt,
                i.PromptTokens,
                i.CompletionTokens,
                i.CostUsd,
                ParseSources(i.MetadataJson)))
            .ToList();
    }

    /// <summary>
    /// Re-hydrates the RAG citations persisted alongside a chat turn. The
    /// AskChatbot handler stores them as <c>{"sources":[…]}</c> in
    /// <see cref="ScholarPath.Domain.Entities.AiInteraction.MetadataJson"/>;
    /// anything else (absent, or malformed JSON) yields an empty list rather
    /// than failing the whole transcript load.
    /// </summary>
    private static IReadOnlyList<ChatSourceDto> ParseSources(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return Array.Empty<ChatSourceDto>();
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<SourcesEnvelope>(metadataJson, SourcesJsonOptions);
            return envelope?.Sources ?? Array.Empty<ChatSourceDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<ChatSourceDto>();
        }
    }

    private sealed record SourcesEnvelope(IReadOnlyList<ChatSourceDto>? Sources);
}
