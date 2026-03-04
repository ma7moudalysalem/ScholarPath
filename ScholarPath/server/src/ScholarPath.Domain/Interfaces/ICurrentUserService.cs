using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    UserRole? UserRole { get; }
    bool IsAuthenticated { get; }
}
