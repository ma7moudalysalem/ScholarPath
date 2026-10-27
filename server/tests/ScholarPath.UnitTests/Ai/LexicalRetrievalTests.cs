using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// With no embedding model configured the assistant retrieves by BM25. These
/// tests pin that it finds the document whose words the question uses, in
/// English and in Arabic, and that its scores stay on the [0, 1] scale the
/// relevance threshold is written for.
/// </summary>
public sealed class LexicalRetrievalTests : IDisposable
{
    private readonly ApplicationDbContext _db = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private void Add(string titleEn, string contentEn, string titleAr = "عنوان", string contentAr = "محتوى")
    {
        _db.KnowledgeDocuments.Add(new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            SourceType = KnowledgeSourceType.Scholarship,
            TitleEn = titleEn,
            ContentEn = contentEn,
            TitleAr = titleAr,
            ContentAr = contentAr,
            EmbeddingModel = LocalEmbeddingService.Model,
            SourceKey = Guid.NewGuid().ToString("N"),
            ContentHash = Guid.NewGuid().ToString("N"),
        });
        _db.SaveChanges();
    }

    private KnowledgeRetriever Retriever() => new(_db, new LocalEmbeddingService());

    [Fact]
    public async Task Finds_the_document_that_shares_the_question_words()
    {
        Add("Masters in Computer Science, Germany", "Fully funded masters award for computer science students in Germany.");
        Add("PhD in Medicine, Canada", "Doctoral funding for medicine in Canada.");
        Add("Bachelor in Law, Egypt", "Partial award for law undergraduates in Egypt.");

        var docs = await Retriever().RetrieveAsync("masters computer science Germany", 4, CancellationToken.None);

        docs[0].TitleEn.Should().Be("Masters in Computer Science, Germany");
        docs.Should().OnlyContain(d => d.Score > 0 && d.Score <= 1);
    }

    [Fact]
    public async Task Answers_an_arabic_question_from_arabic_text()
    {
        Add("Medicine award", "Medicine", "منحة الطب في كندا", "تمويل الدكتوراه في الطب");
        Add("Law award", "Law", "منحة القانون في مصر", "منحة جزئية للقانون");

        var docs = await Retriever().RetrieveAsync("منحة القانون", 4, CancellationToken.None);

        docs[0].TitleEn.Should().Be("Law award");
    }

    [Fact]
    public async Task Returns_nothing_when_no_word_matches()
    {
        Add("Masters in Computer Science", "Germany");

        var docs = await Retriever().RetrieveAsync("astronomy", 4, CancellationToken.None);

        docs.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
