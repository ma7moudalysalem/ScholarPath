namespace ScholarPath.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? ActiveRole { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? CorrelationId { get; }
}

public interface IDateTimeService
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
