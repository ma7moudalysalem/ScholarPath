using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    UserRole? UserRole { get; }
    bool IsAuthenticated { get; }
}
