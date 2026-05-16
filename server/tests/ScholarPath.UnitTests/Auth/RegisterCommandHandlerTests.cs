using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.Register;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

public sealed class RegisterCommandHandlerTests : IDisposable
{
    private const string Email = "new@test.local";

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IDateTimeService _clock = Substitute.For<IDateTimeService>();
    private readonly RegisterCommandHandler _handler;
    private readonly DateTimeOffset _now = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

    public RegisterCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _clock.UtcNow.Returns(_now);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _tokens.IssueTokens(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(new TokenPair("access", "refresh", _now.AddHours(1), _now.AddDays(7)));

        _handler = new RegisterCommandHandler(_db, _hasher, _tokens, _clock);
    }

    private static RegisterCommand Cmd()
        => new(Email, "Passw0rd!", "New", "User", RememberMe: false, IpAddress: null, UserAgent: null);

    [Fact]
    public async Task Handle_NewEmail_CreatesUnassignedUserAndReturnsTokens()
    {
        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        result.AccessToken.Should().Be("access");

        var user = await _db.Users.SingleAsync();
        user.Email.Should().Be(Email);
        user.AccountStatus.Should().Be(AccountStatus.Unassigned);
        user.IsOnboardingComplete.Should().BeFalse();
        user.PasswordHash.Should().Be("hashed");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflict()
    {
        _db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = Email,
            NormalizedEmail = Email.ToUpperInvariant(),
            UserName = Email,
            NormalizedUserName = Email.ToUpperInvariant(),
            FirstName = "Existing",
            LastName = "User",
        });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(Cmd(), CancellationToken.None));
    }

    public void Dispose() => _db.Dispose();
}
