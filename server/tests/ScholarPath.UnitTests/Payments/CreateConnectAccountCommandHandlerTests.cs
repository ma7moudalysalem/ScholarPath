using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.CreateConnectAccount;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public class CreateConnectAccountCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService Consultant(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsInRole("Consultant").Returns(true);
        u.UserId.Returns(id);
        return u;
    }

    private static UserProfile SeedPayee(ApplicationDbContext db, Guid id)
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = id };
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Test",
            LastName = "Consultant",
            Email = "consultant@scholarpath.local",
            UserName = "consultant@scholarpath.local",
            CountryOfResidence = "US",
            Profile = profile,
        });
        return profile;
    }

    private static IStripeService StripeStub()
    {
        var s = Substitute.For<IStripeService>();
        s.CreateConnectAccountAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripeConnectAccountResult($"acct_{Guid.NewGuid():N}", "pending_verification"));
        s.CreateConnectOnboardingLinkAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://stripe/onboard");
        return s;
    }

    private static CreateConnectAccountCommandHandler Sut(
        ApplicationDbContext db, IStripeService stripe, ICurrentUserService user) =>
        new(db, stripe, user, NullLogger<CreateConnectAccountCommandHandler>.Instance);

    private static CreateConnectAccountCommand ValidCommand() =>
        new("https://app.scholarpath.local/return", "https://app.scholarpath.local/refresh");

    [Fact]
    public async Task Creates_connect_account_when_none_exists()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        SeedPayee(db, id);
        await db.SaveChangesAsync();

        var result = await Sut(db, StripeStub(), Consultant(id)).Handle(ValidCommand(), default);

        result.ConnectAccountId.Should().StartWith("acct_");
        result.OnboardingUrl.Should().Be("https://stripe/onboard");
        result.Status.Should().Be(StripeConnectStatus.Pending);

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == id);
        profile.StripeConnectAccountId.Should().NotBeNullOrEmpty();
        profile.StripeConnectStatus.Should().Be(StripeConnectStatus.Pending);
    }

    [Fact]
    public async Task Reuses_existing_connect_account()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        var profile = SeedPayee(db, id);
        profile.StripeConnectAccountId = "acct_existing";
        profile.StripeConnectStatus = StripeConnectStatus.Verified;
        await db.SaveChangesAsync();
        var stripe = StripeStub();

        var result = await Sut(db, stripe, Consultant(id)).Handle(ValidCommand(), default);

        result.ConnectAccountId.Should().Be("acct_existing");
        await stripe.DidNotReceiveWithAnyArgs()
            .CreateConnectAccountAsync(default!, default!, default);
    }

    [Fact]
    public async Task Rejects_non_payee_role()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        SeedPayee(db, id);
        await db.SaveChangesAsync();

        var student = Substitute.For<ICurrentUserService>();
        student.UserId.Returns(id);

        var act = () => Sut(db, StripeStub(), student).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public void Validator_rejects_relative_urls()
    {
        var v = new CreateConnectAccountCommandValidator();

        v.Validate(new CreateConnectAccountCommand("/return", "/refresh"))
            .IsValid.Should().BeFalse();
    }
}
