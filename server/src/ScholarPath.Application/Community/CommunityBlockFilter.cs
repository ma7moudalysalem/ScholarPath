using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Community;

/// <summary>
/// FR-MSG-29: a block is MUTUAL. When two users are in a block relationship (in
/// either direction), neither may view or reply to the other's community content.
/// This is the single predicate every community query + the reply command uses so
/// the block semantics never drift (an earlier version filtered blocker-only, which
/// contradicted the chat side that was already bidirectional).
/// </summary>
public static class CommunityBlockFilter
{
    /// <summary>
    /// A <see cref="ForumPost"/> predicate: true when the post's author is NOT in a
    /// block relationship (either direction) with <paramref name="viewerId"/>.
    /// </summary>
    public static Expression<Func<ForumPost, bool>> NotBlockedWith(IApplicationDbContext db, Guid viewerId) =>
        p => !db.UserBlocks.Any(b =>
            (b.BlockerId == viewerId && b.BlockedUserId == p.AuthorId) ||
            (b.BlockerId == p.AuthorId && b.BlockedUserId == viewerId));

    /// <summary>True when <paramref name="a"/> and <paramref name="b"/> are in a block
    /// relationship in either direction.</summary>
    public static Task<bool> AreBlockedAsync(IApplicationDbContext db, Guid a, Guid b, CancellationToken ct) =>
        db.UserBlocks.AnyAsync(x =>
            (x.BlockerId == a && x.BlockedUserId == b) ||
            (x.BlockerId == b && x.BlockedUserId == a), ct);
}
