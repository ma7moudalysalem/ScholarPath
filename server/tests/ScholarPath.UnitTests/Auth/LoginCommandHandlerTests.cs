using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.Login;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

public sealed class LoginCommandHandlerTests : IDisposable
{
    private const string Email = "student@test.local";

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUserAdministration _userAdmin = Substitute.For<IUserAdministration>();
    private readonly IDateTimeService _clock = Substitute.For<IDateTimeService>();
    private readonly LoginCommandHandler _handler;
    private readonly DateTimeOffset _now = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _clock.UtcNow.Returns(_now);
        _tokens.IssueTokens(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(new TokenPair("access", "refresh", _now.AddHours(1), _now.AddDays(7)));
        _userAdmin.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        _handler = new LoginCommandHandler(_db, _hasher, _tokens, _userAdmin, _clock);
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = Email,
            NormalizedEmail = Email.ToUpperInvariant(),
            UserName = Email,
            NormalizedUserName = Email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "stored-hash",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            AccountStatus = AccountStatus.Active,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private static LoginCommand Cmd(string email = Email)
        => new(email, "Passw0rd!", RememberMe: false, IpAddress: "127.0.0.1", UserAgent: "xunit");

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        await SeedUserAsync();
        _hasher.Verify("stored-hash", "Passw0rd!").Returns(true);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.User.Email.Should().Be(Email);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsConflict()
    {
        await SeedUserAsync();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(Cmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsConflict()
    {
        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(Cmd("nobody@test.local"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_LockedAccount_ThrowsConflict()
    {
        var user = await SeedUserAsync();
        user.LockoutEnd = _now.AddMinutes(10);
        await _db.SaveChangesAsync();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(Cmd(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FifthFailedAttempt_LocksAccount()
    {
        var user = await SeedUserAsync();
        for (var i = 0; i < 4; i++)
        {
            _db.LoginAttempts.Add(new LoginAttempt
            {
                Id = Guid.NewGuid(),
                Email = Email,
                Succeeded = false,
                OccurredAt = _now.AddMinutes(-5),
            });
        }
        await _db.SaveChangesAsync();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(Cmd(), CancellationToken.None));

        user.LockoutEnd.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
