using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Documents.Queries.GetUserOnboardingDocuments;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Documents;

/// <summary>
/// The admin onboarding reviewer lists a Company / Consultant's uploaded
/// verification documents before approving the request (UAT TC-001/002).
/// </summary>
public sealed class GetUserOnboardingDocumentsQueryHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Document Doc(Guid ownerId, DocumentCategory category, DateTimeOffset uploadedAt) => new()
    {
        Id = Guid.NewGuid(),
        OwnerUserId = ownerId,
        FileName = "registration.pdf",
        ContentType = "application/pdf",
        SizeBytes = 2048,
        StoragePath = $"local:documents/{Guid.NewGuid():N}/registration.pdf",
        Category = category,
        UploadedAt = uploadedAt,
    };

    [Fact]
    public async Task Returns_only_the_target_users_onboarding_documents()
    {
        using var db = CreateDb();
        var applicant = Guid.NewGuid();
        var other = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 5, 18, 9, 0, 0, TimeSpan.Zero);
        db.Documents.AddRange(
            Doc(applicant, DocumentCategory.OnboardingDocument, at),
            Doc(applicant, DocumentCategory.Transcript, at),       // wrong category
            Doc(other, DocumentCategory.OnboardingDocument, at));  // wrong owner
        await db.SaveChangesAsync();

        var sut = new GetUserOnboardingDocumentsQueryHandler(db);
        var result = await sut.Handle(new GetUserOnboardingDocumentsQuery(applicant), default);

        result.Should().ContainSingle()
            .Which.Category.Should().Be(DocumentCategory.OnboardingDocument);
    }

    [Fact]
    public async Task Orders_newest_first()
    {
        using var db = CreateDb();
        var applicant = Guid.NewGuid();
        var older = Doc(applicant, DocumentCategory.OnboardingDocument,
            new DateTimeOffset(2026, 5, 17, 9, 0, 0, TimeSpan.Zero));
        var newer = Doc(applicant, DocumentCategory.OnboardingDocument,
            new DateTimeOffset(2026, 5, 18, 9, 0, 0, TimeSpan.Zero));
        db.Documents.AddRange(older, newer);
        await db.SaveChangesAsync();

        var sut = new GetUserOnboardingDocumentsQueryHandler(db);
        var result = await sut.Handle(new GetUserOnboardingDocumentsQuery(applicant), default);

        result.Select(d => d.Id).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Fact]
    public async Task Returns_empty_when_the_user_uploaded_nothing()
    {
        using var db = CreateDb();
        var sut = new GetUserOnboardingDocumentsQueryHandler(db);

        var result = await sut.Handle(new GetUserOnboardingDocumentsQuery(Guid.NewGuid()), default);

        result.Should().BeEmpty();
    }
}
