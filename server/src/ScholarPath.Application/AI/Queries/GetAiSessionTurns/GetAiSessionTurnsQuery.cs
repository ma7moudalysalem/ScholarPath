using MediatR;
using Microsoft.EntityFrameworkCore;
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
    decimal? CostUsd);

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
    public async Task<IReadOnlyList<AiSessionTurnDto>> Handle(
        GetAiSessionTurnsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Array.Empty<AiSessionTurnDto>();
        }

        return await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.SessionId == request.SessionId
                        && i.UserId == userId
                        && i.Feature == AiFeature.Chatbot)
            .OrderBy(i => i.StartedAt)
            .Select(i => new AiSessionTurnDto(
                i.Id,
                i.PromptText,
                i.ResponseText,
                i.StartedAt,
                i.CompletedAt,
                i.PromptTokens,
                i.CompletionTokens,
                i.CostUsd))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
