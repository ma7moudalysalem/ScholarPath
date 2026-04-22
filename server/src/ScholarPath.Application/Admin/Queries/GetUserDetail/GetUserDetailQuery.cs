using MediatR;
using ScholarPath.Application.Admin.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetUserDetail;

public sealed record GetUserDetailQuery(Guid UserId) : IRequest<AdminUserDetail?>;
