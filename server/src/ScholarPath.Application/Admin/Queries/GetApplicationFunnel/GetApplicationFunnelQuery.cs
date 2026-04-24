using MediatR;
using ScholarPath.Application.Admin.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetApplicationFunnel;

public sealed record GetApplicationFunnelQuery : IRequest<IReadOnlyList<ApplicationStatusPoint>>;
