using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.SearchUsers;

public sealed class SearchUsersQueryHandler(IAdminReadService admin)
    : IRequestHandler<SearchUsersQuery, PagedResult<AdminUserRow>>
{
    public Task<PagedResult<AdminUserRow>> Handle(SearchUsersQuery request, CancellationToken ct)
        => admin.SearchUsersAsync(
            request.Search,
            request.Status,
            request.Role,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);
}
