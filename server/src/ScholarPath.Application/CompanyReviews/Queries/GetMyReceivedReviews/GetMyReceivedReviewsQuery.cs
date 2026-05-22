using MediatR;
using ScholarPath.Application.CompanyReviews.DTOs;

namespace ScholarPath.Application.CompanyReviews.Queries.GetMyReceivedReviews;

/// <summary>
/// Returns the reviews the authenticated company has received — masked author
/// names, soft-deleted and admin-hidden rows excluded, newest first — plus an
/// aggregate average and count. The company is resolved from the current user;
/// no id is accepted from the client (a company can only read its own reviews).
/// </summary>
public sealed record GetMyReceivedReviewsQuery : IRequest<ReceivedReviewsSummaryDto>;
