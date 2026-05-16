using MediatR;
using ScholarPath.Application.CompanyReviews.DTOs;

namespace ScholarPath.Application.CompanyReviews.Queries.GetCompanyRatings;

public sealed record GetCompanyRatingsQuery(
    Guid CompanyId,
    int Page = 1,
    int PageSize = 25) : IRequest<CompanyRatingsSummaryDto>;
