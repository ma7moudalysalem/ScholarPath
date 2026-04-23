using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetOnboardingQueue;

public sealed record GetOnboardingQueueQuery(
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<OnboardingRequestRow>>;
