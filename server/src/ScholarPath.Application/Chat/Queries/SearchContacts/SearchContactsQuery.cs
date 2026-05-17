using MediatR;
using ScholarPath.Application.Chat.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Chat.Queries.SearchContacts;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Finds users the current user can start a direct-message conversation with —
/// backs the compose modal's user-picker. Delegates to
/// <see cref="IChatContactReadService"/> because surfacing each user's role
/// needs the Identity join-tables, which are not exposed on
/// <see cref="IApplicationDbContext"/>.
/// </summary>
public sealed record SearchContactsQuery(string? Query)
    : IRequest<IReadOnlyList<ChatContactDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class SearchContactsQueryHandler(
    IChatContactReadService contacts,
    ICurrentUserService currentUser)
    : IRequestHandler<SearchContactsQuery, IReadOnlyList<ChatContactDto>>
{
    /// <summary>Maximum contacts returned per search — keeps the picker snappy.</summary>
    private const int MaxResults = 20;

    public Task<IReadOnlyList<ChatContactDto>> Handle(
        SearchContactsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenAccessException();
        return contacts.SearchContactsAsync(userId, request.Query, MaxResults, ct);
    }
}
