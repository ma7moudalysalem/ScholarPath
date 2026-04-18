using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

public sealed class AuditService(
    ApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock) : IAuditService
{
    public async Task WriteAsync(
        AuditAction action,
        string targetType,
        Guid? targetId,
        string? beforeJson,
        string? afterJson,
        string? summary,
        CancellationToken ct)
    {
        var entry = new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Summary = Trim(summary, 2000),
            IpAddress = Trim(currentUser.IpAddress, 64),
            UserAgent = Trim(currentUser.UserAgent, 512),
            CorrelationId = Trim(currentUser.CorrelationId, 128),
            OccurredAt = clock.UtcNow,
        };

        db.AuditLogs.Add(entry);
        // append-only — bypass the global SaveChanges so audit can land even if
        // the caller already committed; use ExecuteSqlRaw via Add + SaveChangesAsync
        // from a detached perspective. For now the audit row is written as part
        // of the same unit of work as the parent command, which is fine.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static string? Trim(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
