using MediatR;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;

public record GetMyUpgradeRequestQuery : IRequest<UpgradeRequestDetailDto?>;
