using MediatR;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;

namespace ScholarPath.Application.ScholarshipProviderReviews.Queries.GetScholarshipProviderRatings;

public sealed record GetScholarshipProviderRatingsQuery(
    Guid ScholarshipProviderId,
    int Page = 1,
    int PageSize = 25) : IRequest<ScholarshipProviderRatingsSummaryDto>;
