using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Queries.GetPayment;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Queries.GetPayments;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists payments newest-first (PB-013). An Admin sees every payment; any other
/// caller sees only payments where they are the payer or the payee.
/// </summary>
public sealed record GetPaymentsQuery(
    PaymentStatus? Status = null,
    PaymentType? Type = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PaymentDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetPaymentsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    public async Task<PagedResult<PaymentDto>> Handle(
        GetPaymentsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Exclude soft-deleted payments — admins should not see them in the list
        // (PB-013: a soft-deleted payment is treated as if it never existed).
        var query = db.Payments.AsNoTracking().Where(p => !p.IsDeleted);

        // Non-admins are scoped to payments they are a party to.
        if (!currentUser.IsInRole("Admin"))
            query = query.Where(p => p.PayerUserId == userId || p.PayeeUserId == userId);

        if (request.Status is { } status)
            query = query.Where(p => p.Status == status);

        if (request.Type is { } type)
            query = query.Where(p => p.Type == type);

        var total = await query.CountAsync(ct);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = payments
            .Select(p => new PaymentDto(
                p.Id,
                p.Type,
                p.Status,
                p.AmountCents,
                p.Currency,
                p.ProfitShareAmountCents,
                p.PayeeAmountCents,
                p.RefundedAmountCents,
                p.PayerUserId,
                p.PayeeUserId,
                p.StripePaymentIntentId,
                p.StripeChargeId,
                p.RelatedBookingId,
                p.RelatedApplicationId,
                p.HeldAt,
                p.CapturedAt,
                p.RefundedAt,
                p.RefundReason,
                p.FailureReason,
                p.CreatedAt))
            .ToList();

        return new PagedResult<PaymentDto>(items, page, pageSize, total);
    }
}
