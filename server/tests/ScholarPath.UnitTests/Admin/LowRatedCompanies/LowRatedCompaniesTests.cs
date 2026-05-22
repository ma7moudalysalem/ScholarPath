using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ClearCompanyLowRatingFlag;
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

    private async Task<Guid> SeedFlaggedCompanyAsync(
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
            ActiveRole = "Company",
        });
        _db.UserProfiles.Add(new UserProfile
        {
            UserId = id,
            OrganizationLegalName = $"{firstName} LLC",
            CompanyAverageRating = avg,
            CompanyReviewCount = count,
            CompanyLowRatingFlaggedAt = flaggedAt,
        });
        await _db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedUnflaggedCompanyAsync()
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
            ActiveRole = "Company",
        });
        _db.UserProfiles.Add(new UserProfile
        {
            UserId = id,
            CompanyAverageRating = 4.50m,
            CompanyReviewCount = 8,
            CompanyLowRatingFlaggedAt = null,
        });
        await _db.SaveChangesAsync();
        return id;
    }

    // ── GetLowRatedCompaniesQuery ─────────────────────────────────────────────

    [Fact]
    public async Task Query_returns_only_flagged_companies_newest_flag_first()
    {
        var oldFlagId = await SeedFlaggedCompanyAsync(
            1.50m, 4, DateTimeOffset.UtcNow.AddDays(-3), "Older");
        var newFlagId = await SeedFlaggedCompanyAsync(
            2.10m, 5, DateTimeOffset.UtcNow.AddHours(-1), "Newer");
        await SeedUnflaggedCompanyAsync();

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.IsInRole("Admin").Returns(true);
        var sut = new GetLowRatedCompaniesQueryHandler(_db, user);

        var page = await sut.Handle(new GetLowRatedCompaniesQuery(), CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.Total.Should().Be(2);
        // Newest flag first.
        page.Items[0].CompanyId.Should().Be(newFlagId);
        page.Items[1].CompanyId.Should().Be(oldFlagId);
        page.Items[0].AverageRating.Should().Be(2.10m);
        page.Items[0].ReviewCount.Should().Be(5);
        page.Items[0].OrganizationLegalName.Should().Be("Newer LLC");
        page.Items[0].AccountStatus.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task Query_rejects_non_admin_caller()
    {
        await SeedFlaggedCompanyAsync(1.5m, 3, DateTimeOffset.UtcNow);
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
            await SeedFlaggedCompanyAsync(
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
        page1.Items.Select(r => r.CompanyId).Should()
            .NotIntersectWith(page2.Items.Select(r => r.CompanyId));
    }

    // ── ClearCompanyLowRatingFlagCommand ──────────────────────────────────────

    [Fact]
    public async Task Clear_resets_flag_and_returns_true()
    {
        var id = await SeedFlaggedCompanyAsync(1.5m, 3, DateTimeOffset.UtcNow);
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(Guid.NewGuid());
        user.IsInRole("Admin").Returns(true);

        var sut = new ClearCompanyLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearCompanyLowRatingFlagCommandHandler>.Instance);

        var result = await sut.Handle(
            new ClearCompanyLowRatingFlagCommand(id), CancellationToken.None);

        result.Should().BeTrue();
        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == id);
        profile.CompanyLowRatingFlaggedAt.Should().BeNull();
        // Snapshot fields are untouched.
        profile.CompanyAverageRating.Should().Be(1.5m);
        profile.CompanyReviewCount.Should().Be(3);
    }

    [Fact]
    public async Task Clear_is_idempotent_when_already_cleared()
    {
        var id = await SeedUnflaggedCompanyAsync();
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);

        var sut = new ClearCompanyLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearCompanyLowRatingFlagCommandHandler>.Instance);

        var result = await sut.Handle(
            new ClearCompanyLowRatingFlagCommand(id), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_rejects_non_admin_caller()
    {
        var id = await SeedFlaggedCompanyAsync(1.5m, 3, DateTimeOffset.UtcNow);
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole(Arg.Any<string>()).Returns(false);

        var sut = new ClearCompanyLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearCompanyLowRatingFlagCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => sut.Handle(new ClearCompanyLowRatingFlagCommand(id), CancellationToken.None));
    }

    [Fact]
    public async Task Clear_throws_NotFound_when_profile_missing()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);
        var sut = new ClearCompanyLowRatingFlagCommandHandler(
            _db, user, NullLogger<ClearCompanyLowRatingFlagCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(
                new ClearCompanyLowRatingFlagCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void Validator_rejects_empty_company_id()
    {
        var v = new ClearCompanyLowRatingFlagCommandValidator();
        v.Validate(new ClearCompanyLowRatingFlagCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    public void Dispose() => _db.Dispose();
}
