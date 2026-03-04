using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserEmail =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public UserRole? UserRole
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("role")
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

            if (roleClaim is not null && Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role))
                return role;

            return null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
