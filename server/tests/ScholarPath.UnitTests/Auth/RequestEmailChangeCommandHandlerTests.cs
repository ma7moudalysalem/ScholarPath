using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.RequestEmailChange;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Auth;

/// <summary>SRS FR-231 — request-email-change handler behaviour.</summary>
public sealed class RequestEmailChangeCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailChangeService _emailChange = Substitute.For<IEmailChangeService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly RequestEmailChangeCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public RequestEmailChangeCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _currentUser.UserId.Returns(_userId);
        _emailChange.GenerateChangeEmailTokenAsync(_userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("change-token");

        var appOptions = Options.Create(new AppOptions { ClientUrl = "https://app.test" });
        _handler = new RequestEmailChangeCommandHandler(_db, _emailChange, _email, _currentUser, appOptions);
    }

    private ApplicationUser AddUser(string email)
    {
        var user = new ApplicationUser
        {
            Id = _userId,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
        };
        _db.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task Handle_UniqueNewEmail_GeneratesTokenAndSendsEmail()
    {
        AddUser("old@test.com");
        await _db.SaveChangesAsync();

        await _handler.Handle(new RequestEmailChangeCommand("brand-new@test.com"), CancellationToken.None);

        await _emailChange.Received(1).GenerateChangeEmailTokenAsync(
            _userId, "brand-new@test.com", Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "brand-new@test.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAlreadyInUse_ThrowsConflict()
    {
        AddUser("old@test.com");
        _db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "taken@test.com",
            NormalizedEmail = "TAKEN@TEST.COM",
            UserName = "taken@test.com",
            NormalizedUserName = "TAKEN@TEST.COM",
            FirstName = "Other",
            LastName = "Person",
        });
        await _db.SaveChangesAsync();

        await _handler.Awaiting(h => h.Handle(
                new RequestEmailChangeCommand("taken@test.com"), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();

        await _email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameAsCurrentEmail_ThrowsConflict()
    {
        AddUser("same@test.com");
        await _db.SaveChangesAsync();

        await _handler.Awaiting(h => h.Handle(
                new RequestEmailChangeCommand("same@test.com"), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
