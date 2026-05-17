using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Resources;
using ScholarPath.Application.Resources.Queries.SearchResources;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Resources;

public class SearchResourcesQueryHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Resource Make(
        ResourceStatus status, string titleEn = "Guide", string? category = "essays") => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = titleEn,
        TitleAr = "دليل",
        Slug = $"slug-{Guid.NewGuid():N}",
        ContentMarkdownEn = "how to write essays",
        ContentMarkdownAr = "محتوى",
        CategorySlug = category,
        AuthorUserId = Guid.NewGuid(),
        AuthorRole = "Consultant",
        Type = ResourceType.Article,
        Status = status,
        PublishedAt = status == ResourceStatus.Published ? DateTimeOffset.UtcNow : null,
        TagsJson = "[]",
    };

    [Fact]
    public async Task Returns_only_published_resources()
    {
        using var db = CreateDb();
        db.Resources.Add(Make(ResourceStatus.Published));
        db.Resources.Add(Make(ResourceStatus.Draft));
        db.Resources.Add(Make(ResourceStatus.PendingReview));
        db.Resources.Add(Make(ResourceStatus.Hidden));
        await db.SaveChangesAsync();

        var result = await new SearchResourcesQueryHandler(db)
            .Handle(new SearchResourcesQuery(), default);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Filters_by_category()
    {
        using var db = CreateDb();
        db.Resources.Add(Make(ResourceStatus.Published, category: "essays"));
        db.Resources.Add(Make(ResourceStatus.Published, category: "visa"));
        await db.SaveChangesAsync();

        var result = await new SearchResourcesQueryHandler(db)
            .Handle(new SearchResourcesQuery { CategorySlug = "visa" }, default);

        result.TotalCount.Should().Be(1);
        result.Items[0].CategorySlug.Should().Be("visa");
    }

    [Fact]
    public async Task Matches_search_term_in_title()
    {
        using var db = CreateDb();
        db.Resources.Add(Make(ResourceStatus.Published, titleEn: "Scholarship Essay Tips"));
        db.Resources.Add(Make(ResourceStatus.Published, titleEn: "Visa Interview Guide"));
        await db.SaveChangesAsync();

        var result = await new SearchResourcesQueryHandler(db)
            .Handle(new SearchResourcesQuery { Term = "Visa" }, default);

        result.TotalCount.Should().Be(1);
        result.Items[0].TitleEn.Should().Contain("Visa");
    }
}
