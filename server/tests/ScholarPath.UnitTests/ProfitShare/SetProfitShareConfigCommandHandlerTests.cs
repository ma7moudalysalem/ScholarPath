using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ProfitShare.Commands.SetProfitShareConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ProfitShare;

public class SetProfitShareConfigCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService Admin()
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsInRole("Admin").Returns(true);
        u.UserId.Returns(Guid.NewGuid());
        return u;
    }

    private static SetProfitShareConfigCommandHandler Sut(
        ApplicationDbContext db, ICurrentUserService user) =>
        new(db, user, NullLogger<SetProfitShareConfigCommandHandler>.Instance);

    private static ProfitShareConfig ActiveConfig(PaymentType type, decimal pct) => new()
    {
        Id = Guid.NewGuid(),
        PaymentType = type,
        Percentage = pct,
        EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-5),
        EffectiveTo = null,
        SetByAdminId = Guid.NewGuid(),
    };

    [Fact]
    public async Task Creates_first_config_when_none_exists()
    {
        using var db = CreateDb();

        var result = await Sut(db, Admin()).Handle(
            new SetProfitShareConfigCommand(PaymentType.ConsultantBooking, 0.12m), default);

        result.Percentage.Should().Be(0.12m);
        result.IsActive.Should().BeTrue();
        db.ProfitShareConfigs.Count(c => c.EffectiveTo == null).Should().Be(1);
    }

    [Fact]
    public async Task Closes_previous_active_config_and_opens_a_new_one()
    {
        using var db = CreateDb();
        var existing = ActiveConfig(PaymentType.ConsultantBooking, 0.10m);
        db.ProfitShareConfigs.Add(existing);
        await db.SaveChangesAsync();

        await Sut(db, Admin()).Handle(
            new SetProfitShareConfigCommand(PaymentType.ConsultantBooking, 0.20m), default);

        var all = await db.ProfitShareConfigs
            .Where(c => c.PaymentType == PaymentType.ConsultantBooking)
            .ToListAsync();
        all.Should().HaveCount(2);
        all.Count(c => c.EffectiveTo == null).Should().Be(1);
        all.Single(c => c.EffectiveTo == null).Percentage.Should().Be(0.20m);
        all.Single(c => c.Id == existing.Id).EffectiveTo.Should().NotBeNull();
    }

    [Fact]
    public async Task Is_a_no_op_when_percentage_is_unchanged()
    {
        using var db = CreateDb();
        var existing = ActiveConfig(PaymentType.CompanyReview, 0.15m);
        db.ProfitShareConfigs.Add(existing);
        await db.SaveChangesAsync();

        var result = await Sut(db, Admin()).Handle(
            new SetProfitShareConfigCommand(PaymentType.CompanyReview, 0.15m), default);

        result.Id.Should().Be(existing.Id);
        db.ProfitShareConfigs.Count().Should().Be(1);
    }

    [Fact]
    public async Task Rejects_non_admin()
    {
        using var db = CreateDb();
        var notAdmin = Substitute.For<ICurrentUserService>();
        notAdmin.UserId.Returns(Guid.NewGuid());

        var act = () => Sut(db, notAdmin).Handle(
            new SetProfitShareConfigCommand(PaymentType.ConsultantBooking, 0.10m), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public void Validator_rejects_percentage_above_cap()
    {
        var v = new SetProfitShareConfigCommandValidator();

        v.Validate(new SetProfitShareConfigCommand(PaymentType.ConsultantBooking, 0.75m))
            .IsValid.Should().BeFalse();
    }
}
