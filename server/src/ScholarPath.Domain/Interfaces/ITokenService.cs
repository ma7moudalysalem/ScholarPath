using ScholarPath.Domain.Entities;

namespace ScholarPath.Domain.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(ApplicationUser user);

    string GenerateRefreshToken(TimeSpan? extendedExpiry = null);

    Task<bool> ValidateRefreshToken(string token);
}
