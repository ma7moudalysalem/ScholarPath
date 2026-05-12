using ScholarPath.Domain.Interfaces;

namespace ScholarPath.IntegrationTests.Payments;

public sealed class PaymentsTestCurrentUserService : ICurrentUserService
{
    private readonly List<string> _roles = [];

    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public bool IsAuthenticated { get; private set; } = true;
    public string? ActiveRole { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }

    public IReadOnlyCollection<string> Roles => _roles;

    public bool IsInRole(string role) =>
        _roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public void SetUser(Guid userId, string email, params string[] roles)
    {
        UserId = userId;
        Email = email;
        IsAuthenticated = true;
        ActiveRole = roles.FirstOrDefault();
        IpAddress = "127.0.0.1";
        UserAgent = "PaymentsIntegrationTests";
        CorrelationId = Guid.NewGuid().ToString("N");

        _roles.Clear();
        _roles.AddRange(roles);
    }

    public void Clear()
    {
        UserId = null;
        Email = null;
        IsAuthenticated = false;
        ActiveRole = null;
        _roles.Clear();
    }
}
