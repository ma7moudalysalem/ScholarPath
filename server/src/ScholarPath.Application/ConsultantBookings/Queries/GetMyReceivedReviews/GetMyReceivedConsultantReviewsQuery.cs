using MediatR;
using ScholarPath.Application.CompanyReviews.DTOs;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetMyReceivedReviews;

/// <summary>
/// Returns the reviews the authenticated consultant has received — masked
/// author names, soft-deleted and admin-hidden rows excluded, newest first —
/// plus an aggregate average and count. The consultant is resolved from the
/// current user; no id is accepted from the client.
///
/// Reuses <see cref="ReceivedReviewsSummaryDto"/> (declared in the CompanyReviews
/// DTOs) because the company and consultant "reviews received" surfaces share an
/// identical shape — one wire contract, one client component.
/// </summary>
public sealed record GetMyReceivedConsultantReviewsQuery : IRequest<ReceivedReviewsSummaryDto>;
