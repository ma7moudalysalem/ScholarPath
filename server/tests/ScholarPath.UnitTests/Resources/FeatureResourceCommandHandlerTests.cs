using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Resources.Commands.FeatureResource;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Resources;

public class FeatureResourceCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService Admin()
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(Guid.NewGuid());
        u.IsInRole("Admin").Returns(true);
        return u;
    }

    private static FeatureResourceCommandHandler Sut(ApplicationDbContext db) =>
        new(db, Admin(), NullLogger<FeatureResourceCommandHandler>.Instance);

    private static Resource Published(bool featured = false, int order = 0) => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = "T", TitleAr = "ت",
        Slug = $"slug-{Guid.NewGuid():N}",
        AuthorUserId = Guid.NewGuid(),
        AuthorRole = "Admin",
        Type = ResourceType.Article,
        Status = ResourceStatus.Published,
        IsFeatured = featured,
        FeaturedOrder = order,
    };

    [Fact]
    public async Task Features_a_published_resource()
    {
        using var db = CreateDb();
        var resource = Published();
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new FeatureResourceCommand(resource.Id, true), default);

        (await db.Resources.FindAsync(resource.Id))!.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task Rejects_featuring_a_non_published_resource()
    {
        using var db = CreateDb();
        var draft = Published();
        draft.Status = ResourceStatus.Draft;
        db.Resources.Add(draft);
        await db.SaveChangesAsync();

        var act = () => Sut(db).Handle(new FeatureResourceCommand(draft.Id, true), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Enforces_the_six_item_cap()
    {
        using var db = CreateDb();
        for (var i = 0; i < 6; i++)
            db.Resources.Add(Published(featured: true, order: i + 1));
        var seventh = Published();
        db.Resources.Add(seventh);
        await db.SaveChangesAsync();

        var act = () => Sut(db).Handle(new FeatureResourceCommand(seventh.Id, true), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Unfeaturing_clears_the_flag()
    {
        using var db = CreateDb();
        var resource = Published(featured: true, order: 1);
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new FeatureResourceCommand(resource.Id, false), default);

        var updated = await db.Resources.FindAsync(resource.Id);
        updated!.IsFeatured.Should().BeFalse();
        updated.FeaturedOrder.Should().Be(0);
    }
}
