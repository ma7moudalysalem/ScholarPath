using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Queries.GetUpgradeQueue;

public sealed record GetUpgradeQueueQuery(
    UpgradeRequestStatus? Status = UpgradeRequestStatus.Pending,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<UpgradeRequestRow>>;
