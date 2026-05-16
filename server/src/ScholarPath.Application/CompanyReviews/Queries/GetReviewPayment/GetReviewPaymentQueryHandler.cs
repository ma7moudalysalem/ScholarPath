using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.CompanyReviews.Queries.GetReviewPayment;

public sealed class GetReviewPaymentQueryHandler(
    IApplicationDbContext db)
    : IRequestHandler<GetReviewPaymentQuery, ReviewPaymentDto?>
{
    public async Task<ReviewPaymentDto?> Handle(GetReviewPaymentQuery request, CancellationToken ct)
    {
        var payment = await db.CompanyReviewPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationTrackerId == request.ApplicationId, ct);

        if (payment == null) return null;

        return new ReviewPaymentDto(
            payment.Id,
            payment.ApplicationTrackerId,
            payment.CompanyId,
            payment.AmountUsd,
            payment.Status,
            payment.CapturedAt,
            payment.RefundedAmountUsd,
            payment.RefundReason);
    }
}
