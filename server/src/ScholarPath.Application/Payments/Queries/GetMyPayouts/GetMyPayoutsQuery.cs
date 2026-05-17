using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Payments.Queries.GetMyPayouts;

// ─── DTO ──────────────────────────────────────────────────────────────────────

public sealed record PayoutDto(
    Guid Id,
    long AmountCents,
    string Currency,
    PayoutStatus Status,
    string? StripePayoutId,
    int IncludedPaymentCount,
    DateTimeOffset? InitiatedAt,
    DateTimeOffset? PaidAt,
    string? FailureReason,
    DateTimeOffset CreatedAt);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>Returns the authenticated payee's own payouts, newest first (PB-013).</summary>
public sealed record GetMyPayoutsQuery : IRequest<IReadOnlyList<PayoutDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyPayoutsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyPayoutsQuery, IReadOnlyList<PayoutDto>>
{
    public async Task<IReadOnlyList<PayoutDto>> Handle(
        GetMyPayoutsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var payouts = await db.Payouts
            .AsNoTracking()
            .Where(p => p.PayeeUserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return payouts
            .Select(p => new PayoutDto(
                p.Id,
                p.AmountCents,
                p.Currency,
                p.Status,
                p.StripePayoutId,
                CountIncludedPayments(p.IncludedPaymentIdsJson),
                p.InitiatedAt,
                p.PaidAt,
                p.FailureReason,
                p.CreatedAt))
            .ToList();
    }

    private static int CountIncludedPayments(string? json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json)?.Count ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
