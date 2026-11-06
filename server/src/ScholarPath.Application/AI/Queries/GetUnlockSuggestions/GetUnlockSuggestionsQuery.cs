using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Queries.GetUnlockSuggestions;

/// <summary>
/// The single profile changes (a destination to add, a level to reach) that would
/// make the current student fully eligible for more open listings, each with the
/// number of listings it would open. Deterministic: read from the eligibility rules.
/// </summary>
public sealed record GetUnlockSuggestionsQuery(int TopN = 5) : IRequest<IReadOnlyList<AiUnlockSuggestion>>;

public sealed class GetUnlockSuggestionsQueryHandler(IAiService ai, ICurrentUserService currentUser)
    : IRequestHandler<GetUnlockSuggestionsQuery, IReadOnlyList<AiUnlockSuggestion>>
{
    public Task<IReadOnlyList<AiUnlockSuggestion>> Handle(GetUnlockSuggestionsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenAccessException("Not authenticated.");
        return ai.SuggestUnlocksAsync(userId, request.TopN, ct);
    }
}
