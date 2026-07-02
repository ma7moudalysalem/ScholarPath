using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.UnitTests.ScholarshipProviderReviewRequests;
using Xunit;

namespace ScholarPath.UnitTests.Scholarships;

/// <summary>
/// The <c>payments.allowFreeScholarships</c> platform setting (PB-005R) lets an
/// admin disable free in-app scholarships system-wide. When off, a ScholarshipProvider
/// cannot set the Review Service Fee to 0 — the validators still allow 0
/// syntactically (the live policy may change between requests), so enforcement
/// lives in the create / update / configure handlers.
/// </summary>
public class AllowFreeScholarshipsSettingTests
{
    private static void SeedSetting(
        Infrastructure.Persistence.ApplicationDbContext db,
        bool allowed)
    {
        db.PlatformSettings.Add(new PlatformSetting
        {
            Id = Guid.NewGuid(),
            Key = PlatformSettingsKeys.AllowFreeScholarships,
            Value = allowed ? "true" : "false",
            ValueType = PlatformSettingType.Boolean,
            Category = "Payments",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task ConfigureReviewFee_with_zero_succeeds_when_setting_is_enabled()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 50m);
        SeedSetting(db, allowed: true);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new ConfigureReviewFeeCommandHandler(
            db, currentUser, NullLogger<ConfigureReviewFeeCommandHandler>.Instance);

        await sut.Handle(new ConfigureReviewFeeCommand(scholarship.Id, 0m), default);

        db.Scholarships.Single(s => s.Id == scholarship.Id)
            .ReviewFeeUsd.Should().Be(0m);
    }

    [Fact]
    public async Task ConfigureReviewFee_with_zero_throws_when_setting_is_disabled()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 50m);
        SeedSetting(db, allowed: false);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new ConfigureReviewFeeCommandHandler(
            db, currentUser, NullLogger<ConfigureReviewFeeCommandHandler>.Instance);

        var act = () => sut.Handle(
            new ConfigureReviewFeeCommand(scholarship.Id, 0m), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Free in-app scholarships are not enabled*");

        // Existing fee must NOT have been overwritten when the gate trips.
        db.Scholarships.Single(s => s.Id == scholarship.Id)
            .ReviewFeeUsd.Should().Be(50m);
    }

    [Fact]
    public async Task ConfigureReviewFee_with_positive_value_is_unaffected_by_setting()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 50m);
        SeedSetting(db, allowed: false);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new ConfigureReviewFeeCommandHandler(
            db, currentUser, NullLogger<ConfigureReviewFeeCommandHandler>.Instance);

        await sut.Handle(new ConfigureReviewFeeCommand(scholarship.Id, 120m), default);

        db.Scholarships.Single(s => s.Id == scholarship.Id)
            .ReviewFeeUsd.Should().Be(120m);
    }

    [Fact]
    public async Task ConfigureReviewFee_with_zero_defaults_to_allowed_when_setting_missing()
    {
        // Missing setting row → default is true (don't strand existing data
        // when an admin hasn't touched the new key yet).
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 50m);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new ConfigureReviewFeeCommandHandler(
            db, currentUser, NullLogger<ConfigureReviewFeeCommandHandler>.Instance);

        await sut.Handle(new ConfigureReviewFeeCommand(scholarship.Id, 0m), default);

        db.Scholarships.Single(s => s.Id == scholarship.Id)
            .ReviewFeeUsd.Should().Be(0m);
    }
}
