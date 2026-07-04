using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Payments.Queries.GetPayment;

// ─── DTO ──────────────────────────────────────────────────────────────────────

public sealed record PaymentDto(
    Guid Id,
    PaymentType Type,
    PaymentStatus Status,
    long AmountCents,
    string Currency,
    long ProfitShareAmountCents,
    long PayeeAmountCents,
    long RefundedAmountCents,
    Guid PayerUserId,
    Guid? PayeeUserId,
    string? StripePaymentIntentId,
    string? StripeChargeId,
    Guid? RelatedBookingId,
    Guid? RelatedApplicationId,
    DateTimeOffset? HeldAt,
    DateTimeOffset? CapturedAt,
    DateTimeOffset? RefundedAt,
    string? RefundReason,
    string? FailureReason,
    DateTimeOffset CreatedAt);

// ─── Query ────────────────────────────────────────────────────────────────────

public sealed record GetPaymentQuery(Guid PaymentId) : IRequest<PaymentDto?>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetPaymentQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPaymentQuery, PaymentDto?>
{
    public async Task<PaymentDto?> Handle(
        GetPaymentQuery request,
        CancellationToken ct)
    {
        var payment = await db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment is null)
            return null;

        // Only the payer, the payee, or an admin may read a payment.
        if (payment.PayerUserId != currentUser.UserId
            && payment.PayeeUserId != currentUser.UserId
            && !currentUser.IsAdminOrSuperAdmin())
        {
            throw new ForbiddenAccessException("You can only view your own payments.");
        }

        return new PaymentDto(
            Id: payment.Id,
            Type: payment.Type,
            Status: payment.Status,
            AmountCents: payment.AmountCents,
            Currency: payment.Currency,
            ProfitShareAmountCents: payment.ProfitShareAmountCents,
            PayeeAmountCents: payment.PayeeAmountCents,
            RefundedAmountCents: payment.RefundedAmountCents,
            PayerUserId: payment.PayerUserId,
            PayeeUserId: payment.PayeeUserId,
            StripePaymentIntentId: payment.StripePaymentIntentId,
            StripeChargeId: payment.StripeChargeId,
            RelatedBookingId: payment.RelatedBookingId,
            RelatedApplicationId: payment.RelatedApplicationId,
            HeldAt: payment.HeldAt,
            CapturedAt: payment.CapturedAt,
            RefundedAt: payment.RefundedAt,
            RefundReason: payment.RefundReason,
            FailureReason: payment.FailureReason,
            CreatedAt: payment.CreatedAt);
    }
}
