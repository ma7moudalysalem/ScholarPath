using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public string? ActiveRole => _accessor.HttpContext?.User.FindFirstValue("active_role");

    public IReadOnlyCollection<string> Roles =>
        _accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? [];

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => _accessor.HttpContext?.User.IsInRole(role) ?? false;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId =>
        _accessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? _accessor.HttpContext?.TraceIdentifier;
}
