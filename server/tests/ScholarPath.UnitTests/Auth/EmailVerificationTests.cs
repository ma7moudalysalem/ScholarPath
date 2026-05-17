using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.ResendVerificationEmail;
using ScholarPath.Application.Auth.Commands.VerifyEmail;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

public sealed class EmailVerificationTests
{
    private const string Email = "verify@test.local";

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser UnverifiedUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = Email,
        NormalizedEmail = Email.ToUpperInvariant(),
        UserName = Email,
        NormalizedUserName = Email.ToUpperInvariant(),
        FirstName = "Test",
        LastName = "User",
        EmailConfirmed = false,
    };

    // ─── VerifyEmail ────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_succeeds_with_valid_token()
    {
        var verification = Substitute.For<IEmailVerificationService>();
        var userId = Guid.NewGuid();
        verification.ConfirmEmailAsync(userId, "good-token", Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new VerifyEmailCommandHandler(verification);
        var act = () => sut.Handle(new VerifyEmailCommand(userId, "good-token"), default);

        await act.Should().NotThrowAsync();
        await verification.Received(1)
            .ConfirmEmailAsync(userId, "good-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEmail_throws_conflict_on_invalid_token()
    {
        var verification = Substitute.For<IEmailVerificationService>();
        verification.ConfirmEmailAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = new VerifyEmailCommandHandler(verification);
        var act = () => sut.Handle(new VerifyEmailCommand(Guid.NewGuid(), "bad"), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public void VerifyEmail_validator_requires_userId_and_token()
    {
        var v = new VerifyEmailCommandValidator();

        v.Validate(new VerifyEmailCommand(Guid.Empty, "t")).IsValid.Should().BeFalse();
        v.Validate(new VerifyEmailCommand(Guid.NewGuid(), "")).IsValid.Should().BeFalse();
        v.Validate(new VerifyEmailCommand(Guid.NewGuid(), "t")).IsValid.Should().BeTrue();
    }

    // ─── ResendVerificationEmail ────────────────────────────────────────────

    [Fact]
    public async Task Resend_sends_email_for_unverified_user()
    {
        using var db = CreateDb();
        var user = UnverifiedUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var verification = Substitute.For<IEmailVerificationService>();
        verification.GenerateConfirmationTokenAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns("token");
        var email = Substitute.For<IEmailService>();

        var sut = new ResendVerificationEmailCommandHandler(
            db, verification, email, Options.Create(new AppOptions()));
        await sut.Handle(new ResendVerificationEmailCommand(Email), default);

        await email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == Email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resend_is_silent_noop_for_already_verified_user()
    {
        using var db = CreateDb();
        var user = UnverifiedUser();
        user.EmailConfirmed = true;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var verification = Substitute.For<IEmailVerificationService>();
        var email = Substitute.For<IEmailService>();

        var sut = new ResendVerificationEmailCommandHandler(
            db, verification, email, Options.Create(new AppOptions()));
        await sut.Handle(new ResendVerificationEmailCommand(Email), default);

        await email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resend_is_silent_noop_for_unknown_email()
    {
        using var db = CreateDb();
        var verification = Substitute.For<IEmailVerificationService>();
        var email = Substitute.For<IEmailService>();

        var sut = new ResendVerificationEmailCommandHandler(
            db, verification, email, Options.Create(new AppOptions()));
        var act = () => sut.Handle(
            new ResendVerificationEmailCommand("nobody@test.local"), default);

        await act.Should().NotThrowAsync();
        await email.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Resend_validator_requires_valid_email()
    {
        var v = new ResendVerificationEmailCommandValidator();

        v.Validate(new ResendVerificationEmailCommand("")).IsValid.Should().BeFalse();
        v.Validate(new ResendVerificationEmailCommand("not-an-email")).IsValid.Should().BeFalse();
        v.Validate(new ResendVerificationEmailCommand(Email)).IsValid.Should().BeTrue();
    }
}
