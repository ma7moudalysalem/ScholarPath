using MediatR;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Queries.GetReviewPayment;

public record ReviewPaymentDto(
    Guid PaymentId,
    Guid ApplicationId,
    Guid CompanyId,
    decimal AmountUsd,
    PaymentStatus Status,
    DateTimeOffset? CapturedAt,
    decimal? RefundedAmountUsd,
    string? RefundReason);

public sealed record GetReviewPaymentQuery(
    Guid ApplicationId) : IRequest<ReviewPaymentDto?>;
