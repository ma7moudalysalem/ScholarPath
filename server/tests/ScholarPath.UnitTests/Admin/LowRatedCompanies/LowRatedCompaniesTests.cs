using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ClearScholarshipProviderLowRatingFlag;
using ScholarPath.Application.Admin.Queries.GetLowRatedCompanies;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Admin.LowRatedCompanies;

public sealed class LowRatedCompaniesTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public LowRatedCompaniesTests()
    {
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private async Task<Guid> SeedFlaggedScholarshipProviderAsync(
        decimal? avg, int count, DateTimeOffset flaggedAt, string firstName = "Acme")
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = firstName,
            LastName = "Foundation",
#pragma warning disable CA1308 // emails are by convention lowercase, never used for case-insensitive comparison here
            Email = $"{firstName.ToLowerInvariant()}-{id:N}@example.com",
            UserName = $"{firstName.ToLowerInvariant()}-{id:N}@example.com",
#pragma warning restore CA1308
            AccountStatus = AccountStatus.Active,
            ActiveRole = "ScholarshipProvider",
        });
        _db.UserProfiles.Add(new UserProfile
        {
            UserId = id,
            OrganizationLegalName = $"{firstName} LLC",
            ScholarshipProviderAverageRating = avg,
            ScholarshipProviderReviewCount = count,
            ScholarshipProviderLowRatingFlaggedAt = flaggedAt,
        });
        await _db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedUnflaggedScholarshipProviderAsync()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Healthy",
            LastName = "Co",
            Email = $"healthy-{id:N}@example.com",
            UserName = $"healthy-{id:N}@example.com",
            AccountStatus = AccountStatus.Active,
            ActiveRole = "ScholarshipProvider",
        });
        _db.UserProfiles.Add(new UserProfile
        {
            UserId = id,
            ScholarshipProviderAverageRating = 4.50m,
            ScholarshipProviderReviewCount = 8,
            ScholarshipProviderLowRatingFlaggedAt = null,
        });
        await _db.SaveChangesAsync();
        return id;
    }

    // ── GetLowRatedCompaniesQuery ─────────────────────────────────────────────

    [Fact]
    public async Task Query_returns_only_flagged_companies_newest_flag_first()
    {
        var oldFlagId = await SeedFlaggedScholarshipProviderAsync(
            1.50m, 4, DateTimeOffset.UtcNow.AddDays(-3), "Older");
        var newFlagId = await SeedFlaggedScholarshipProviderAsync(
            2.10m, 5, DateTimeOffset.UtcNow.AddHours(-1), "Newer");
        await SeedUnflaggedScholarshipProviderAsync();

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.IsInRole("Admin").Returns(true);
        var sut = new GetLowRatedCompaniesQueryHandler(_db, user);

        var page = await sut.Handle(new GetLowRatedCompaniesQuery(), CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.Total.Should().Be(2);
        // Newest flag first.
        page.Items[0].ScholarshipProviderId.Should().Be(newFlagId);
        page.Items[1].ScholarshipProviderId.Should().Be(oldFlagId);
        page.Items[0].AverageRating.Should().Be(2.10m);
        page.Items[0].ReviewCount.Should().Be(5);
        page.Items[0].OrganizationLegalName.Should().Be("Newer LLC");
        page.Items[0].AccountStatus.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task Query_rejects_non_admin_caller()
    {
        await SeedFlaggedScholarshipProviderAsync(1.5m, 3, DateTimeOffset.UtcNow);
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole(Arg.Any<string>()).Returns(false);

        var sut = new GetLowRatedCompaniesQueryHandler(_db, user);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Handle(new GetLowRatedCompaniesQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Query_paginates_results()
    {
        for (int i = 0; i < 5; i++)
        {
            await SeedFlaggedScholarshipProviderAsync(
                1.0m, 1, DateTimeOffset.UtcNow.AddMinutes(-i), $"Co{i}");
        }

        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);
        var sut = new GetLowRatedCompaniesQueryHandler(_db, user);

        var page1 = await sut.Handle(
            new GetLowRatedCompaniesQuery(Page: 1, PageSize: 2), CancellationToken.None);
        var page2 = await sut.Handle(
            new GetLowRatedCompaniesQuery(Page: 2, PageSize: 2), CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.Total.Should().Be(5);
        page2.Total.Should().Be(5);
        page1.Items.Select(r => r.ScholarshipProviderId).Should()
            .NotIntersectWith(page2.Items.Select(r => r.ScholarshipProviderId));
    }

    // ── ClearScholarshipProviderLowRatingFlagCommand ──────────────────────────────────────

    [Fact]
    public async Task Clear_resets_flag_and_returns_true()
    {
        var id = await SeedFlaggedScholarshipProviderAsync(1.5m, 3, DateTimeOffset.UtcNow);
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.IsInRole("Admin").Returns(true);

        var sut = new ClearScholarshipProviderLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearScholarshipProviderLowRatingFlagCommandHandler>.Instance);

        var result = await sut.Handle(
            new ClearScholarshipProviderLowRatingFlagCommand(id), CancellationToken.None);

        result.Should().BeTrue();
        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == id);
        profile.ScholarshipProviderLowRatingFlaggedAt.Should().BeNull();
        // Snapshot fields are untouched.
        profile.ScholarshipProviderAverageRating.Should().Be(1.5m);
        profile.ScholarshipProviderReviewCount.Should().Be(3);
    }

    [Fact]
    public async Task Clear_is_idempotent_when_already_cleared()
    {
        var id = await SeedUnflaggedScholarshipProviderAsync();
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);

        var sut = new ClearScholarshipProviderLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearScholarshipProviderLowRatingFlagCommandHandler>.Instance);

        var result = await sut.Handle(
            new ClearScholarshipProviderLowRatingFlagCommand(id), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_rejects_non_admin_caller()
    {
        var id = await SeedFlaggedScholarshipProviderAsync(1.5m, 3, DateTimeOffset.UtcNow);
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole(Arg.Any<string>()).Returns(false);

        var sut = new ClearScholarshipProviderLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearScholarshipProviderLowRatingFlagCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Handle(new ClearScholarshipProviderLowRatingFlagCommand(id), CancellationToken.None));
    }

    [Fact]
    public async Task Clear_throws_NotFound_when_profile_missing()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);
        var sut = new ClearScholarshipProviderLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearScholarshipProviderLowRatingFlagCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(
                new ClearScholarshipProviderLowRatingFlagCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void Validator_rejects_empty_company_id()
    {
        var v = new ClearScholarshipProviderLowRatingFlagCommandValidator();
        v.Validate(new ClearScholarshipProviderLowRatingFlagCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    public void Dispose() => _db.Dispose();
}
