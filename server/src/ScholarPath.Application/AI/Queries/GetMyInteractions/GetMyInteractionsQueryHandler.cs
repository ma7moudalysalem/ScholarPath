using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Queries.GetMyInteractions;

public sealed class GetMyInteractionsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyInteractionsQuery, IReadOnlyList<AiInteractionRowDto>>
{
    public async Task<IReadOnlyList<AiInteractionRowDto>> Handle(GetMyInteractionsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var limit = Math.Clamp(request.Limit, 1, 100);

        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.StartedAt)
            .Take(limit)
            .Select(i => new AiInteractionRowDto(
                i.Id,
                i.Feature,
                i.ModelName,
                i.StartedAt,
                i.CompletedAt,
                i.PromptTokens,
                i.CompletionTokens,
                i.CostUsd,
                i.ErrorMessage == null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }
}
