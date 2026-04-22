using MediatR;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Queries.SearchUsers;

public sealed record SearchUsersQuery(
    string? Search,
    AccountStatus? Status,
    string? Role,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<AdminUserRow>>;
