using ScholarPath.Application.Auth.Commands.Logout;
using ScholarPath.Domain.Entities;

namespace ScholarPath.UnitTests;

public class LogoutCommandHandlerTests
{
    [Fact]
    public void LogoutCommand_has_correct_default_values()
    {
        var command = new LogoutCommand("test-token");

        Assert.Equal("test-token", command.RefreshToken);
        Assert.False(command.LogoutEverywhere);
    }

    [Fact]
    public void LogoutCommand_LogoutEverywhere_can_be_set_to_true()
    {
        var command = new LogoutCommand("test-token", LogoutEverywhere: true);

        Assert.Equal("test-token", command.RefreshToken);
        Assert.True(command.LogoutEverywhere);
    }

    [Fact]
    public void RefreshToken_IsRevoked_returns_true_when_RevokedAt_is_set()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_IsRevoked_returns_false_when_RevokedAt_is_null()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_IsExpired_returns_true_when_past_expiry()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        Assert.True(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_RevokedReason_can_be_set()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow,
            RevokedReason = "User logout"
        };

        Assert.Equal("User logout", token.RevokedReason);
        Assert.True(token.IsRevoked);
    }
}