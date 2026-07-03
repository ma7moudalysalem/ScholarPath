using System.Net.Http;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;
using Xunit;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// When the embedding provider is down or misconfigured, a knowledge-base
/// rebuild must fail with an actionable <see cref="ServiceUnavailableException"/>
/// (surfaced as HTTP 503) rather than an opaque 500.
/// </summary>
public sealed class KnowledgeBaseIndexerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Rebuild_throws_ServiceUnavailable_when_the_embedding_provider_fails()
    {
        using var db = CreateDb();

        var embeddings = Substitute.For<IEmbeddingService>();
        embeddings.ModelName.Returns("azure:text-embedding-3-small");
        embeddings.EmbedBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("401 Unauthorized"));

        // One FAQ document so the rebuild has something to embed.
        var datasets = Substitute.For<IDatasetProvider>();
        datasets.GetDatasetJson("scholarpath-faq").Returns(
            "{\"faqs\":[{\"key\":\"q1\",\"questionEn\":\"Q\",\"questionAr\":\"س\"," +
            "\"answerEn\":\"A\",\"answerAr\":\"ج\"}]}");

        var indexer = new KnowledgeBaseIndexer(
            db, embeddings, datasets,
            Options.Create(new AiOptions()),
            NullLogger<KnowledgeBaseIndexer>.Instance);

        var act = () => indexer.RebuildAsync(force: false, CancellationToken.None);

        (await act.Should().ThrowAsync<ServiceUnavailableException>())
            .Which.Message.Should().Contain("azure:text-embedding-3-small");
    }
}
