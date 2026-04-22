using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetUserDetail;

public sealed class GetUserDetailQueryHandler(IAdminReadService admin)
    : IRequestHandler<GetUserDetailQuery, AdminUserDetail?>
{
    public Task<AdminUserDetail?> Handle(GetUserDetailQuery request, CancellationToken ct)
        => admin.GetUserDetailAsync(request.UserId, ct);
}
