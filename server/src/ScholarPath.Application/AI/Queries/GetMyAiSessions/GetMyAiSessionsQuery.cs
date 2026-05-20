using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Queries.GetMyAiSessions;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// One row in the user's "past AI chats" sidebar. The title is a short preview
/// of the very first prompt in the session so the user recognises the chat at
/// a glance ("scholarships for software engineering", "MIT eligibility", …).
/// </summary>
public sealed record AiSessionSummaryDto(
    string SessionId,
    string Title,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastTurnAt,
    int TurnCount);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists every AI chat session the authenticated user has had, newest-first.
/// Backs the chatbot's left rail — users browse past chats, click one to
/// resume (the handler then replays its turns into the LLM as memory).
/// </summary>
public sealed record GetMyAiSessionsQuery : IRequest<IReadOnlyList<AiSessionSummaryDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyAiSessionsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyAiSessionsQuery, IReadOnlyList<AiSessionSummaryDto>>
{
    /// <summary>Max char preview used as the session title in the sidebar.</summary>
    private const int TitleMaxLength = 80;

    public async Task<IReadOnlyList<AiSessionSummaryDto>> Handle(
        GetMyAiSessionsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Roll up the user's chatbot interactions into one row per SessionId
        // — the earliest StartedAt anchors the chronology, the latest is the
        // "last activity" timestamp, and the count is the turn total. The
        // title is the first non-empty prompt, taken from the first turn.
        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId
                        && i.Feature == AiFeature.Chatbot
                        && i.SessionId != null)
            .GroupBy(i => i.SessionId!)
            .Select(g => new
            {
                SessionId = g.Key,
                StartedAt = g.Min(i => i.StartedAt),
                LastTurnAt = g.Max(i => (DateTimeOffset?)i.CompletedAt) ?? g.Max(i => i.StartedAt),
                TurnCount = g.Count(),
                FirstPrompt = g
                    .OrderBy(i => i.StartedAt)
                    .Select(i => i.PromptText)
                    .FirstOrDefault(),
            })
            .OrderByDescending(g => g.LastTurnAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .Select(r => new AiSessionSummaryDto(
                SessionId: r.SessionId,
                Title: BuildTitle(r.FirstPrompt),
                StartedAt: r.StartedAt,
                LastTurnAt: r.LastTurnAt,
                TurnCount: r.TurnCount))
            .ToList();
    }

    /// <summary>Trims and truncates the first prompt to a sidebar-friendly title.</summary>
    private static string BuildTitle(string? firstPrompt)
    {
        if (string.IsNullOrWhiteSpace(firstPrompt)) return "Untitled chat";
        var trimmed = firstPrompt.Trim().ReplaceLineEndings(" ");
        if (trimmed.Length <= TitleMaxLength) return trimmed;
        return trimmed[..TitleMaxLength].TrimEnd() + "…";
    }
}
