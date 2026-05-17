using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Documents.Commands.DeleteDocument;
using ScholarPath.Application.Documents.Commands.UploadDocument;
using ScholarPath.Application.Documents.Queries.DownloadDocument;
using ScholarPath.Application.Documents.Queries.GetMyDocuments;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Documents;

public sealed class DocumentVaultTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 17, 10, 0, 0, TimeSpan.Zero);

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService User(Guid id, params string[] roles)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        foreach (var r in roles) u.IsInRole(r).Returns(true);
        return u;
    }

    private static IDateTimeService Clock()
    {
        var c = Substitute.For<IDateTimeService>();
        c.UtcNow.Returns(Now);
        return c;
    }

    private static IBlobStorageService Storage(string uploadPath = "local:documents/key/file.pdf")
    {
        var s = Substitute.For<IBlobStorageService>();
        s.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(uploadPath);
        return s;
    }

    private static MemoryStream Bytes(string content = "file-content") =>
        new(Encoding.UTF8.GetBytes(content));

    private static UploadDocumentCommand UploadCmd(
        Stream content, DocumentCategory category = DocumentCategory.Transcript, Guid? appId = null) =>
        new(content, "transcript.pdf", "application/pdf", content.Length, category, appId);

    // ─── Upload ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_persists_metadata_and_stores_bytes()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var storage = Storage();
        var sut = new UploadDocumentCommandHandler(
            db, User(userId), storage, Clock(),
            NullLogger<UploadDocumentCommandHandler>.Instance);

        using var content = Bytes();
        var result = await sut.Handle(UploadCmd(content), default);

        result.FileName.Should().Be("transcript.pdf");
        result.Category.Should().Be(DocumentCategory.Transcript);

        var saved = await db.Documents.SingleAsync();
        saved.OwnerUserId.Should().Be(userId);
        saved.StoragePath.Should().Be("local:documents/key/file.pdf");
        saved.UploadedAt.Should().Be(Now);
        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), "transcript.pdf", "application/pdf", "documents",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_rejects_disallowed_extension()
    {
        using var db = CreateDb();
        var sut = new UploadDocumentCommandHandler(
            db, User(Guid.NewGuid()), Storage(), Clock(),
            NullLogger<UploadDocumentCommandHandler>.Instance);

        using var content = Bytes();
        var act = () => sut.Handle(
            new UploadDocumentCommand(content, "malware.exe", "application/octet-stream",
                content.Length, DocumentCategory.Other, null),
            default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Upload_rejects_link_to_another_users_application()
    {
        using var db = CreateDb();
        var appId = Guid.NewGuid();
        db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = Guid.NewGuid(), // a different student
            ScholarshipId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var sut = new UploadDocumentCommandHandler(
            db, User(Guid.NewGuid()), Storage(), Clock(),
            NullLogger<UploadDocumentCommandHandler>.Instance);

        using var content = Bytes();
        var act = () => sut.Handle(UploadCmd(content, appId: appId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validator_rejects_oversize_and_empty_files()
    {
        var v = new UploadDocumentCommandValidator();
        using var content = Bytes();

        v.Validate(new UploadDocumentCommand(content, "a.pdf", "application/pdf",
            0, DocumentCategory.Other, null)).IsValid.Should().BeFalse();
        v.Validate(new UploadDocumentCommand(content, "a.pdf", "application/pdf",
            UploadDocumentCommandHandler.MaxBytes + 1, DocumentCategory.Other, null))
            .IsValid.Should().BeFalse();
        v.Validate(new UploadDocumentCommand(content, "a.pdf", "application/pdf",
            1024, DocumentCategory.Other, null)).IsValid.Should().BeTrue();
    }

    // ─── List ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyDocuments_returns_only_callers_documents()
    {
        using var db = CreateDb();
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        db.Documents.AddRange(
            Doc(me, DocumentCategory.Transcript),
            Doc(me, DocumentCategory.Resume),
            Doc(other, DocumentCategory.Transcript));
        await db.SaveChangesAsync();

        var sut = new GetMyDocumentsQueryHandler(db, User(me));
        var result = await sut.Handle(new GetMyDocumentsQuery(), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMyDocuments_filters_by_category()
    {
        using var db = CreateDb();
        var me = Guid.NewGuid();
        db.Documents.AddRange(
            Doc(me, DocumentCategory.Transcript),
            Doc(me, DocumentCategory.Resume));
        await db.SaveChangesAsync();

        var sut = new GetMyDocumentsQueryHandler(db, User(me));
        var result = await sut.Handle(new GetMyDocumentsQuery(DocumentCategory.Resume), default);

        result.Should().ContainSingle()
            .Which.Category.Should().Be(DocumentCategory.Resume);
    }

    // ─── Download ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Download_returns_bytes_for_owner()
    {
        using var db = CreateDb();
        var me = Guid.NewGuid();
        var doc = Doc(me, DocumentCategory.Transcript);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var storage = Substitute.For<IBlobStorageService>();
        storage.DownloadAsync(doc.StoragePath, Arg.Any<CancellationToken>())
            .Returns(Bytes("the-bytes"));

        var sut = new DownloadDocumentQueryHandler(db, User(me), storage);
        var result = await sut.Handle(new DownloadDocumentQuery(doc.Id), default);

        result.FileName.Should().Be(doc.FileName);
        result.ContentType.Should().Be(doc.ContentType);
    }

    [Fact]
    public async Task Download_forbidden_for_non_owner()
    {
        using var db = CreateDb();
        var doc = Doc(Guid.NewGuid(), DocumentCategory.Transcript);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var sut = new DownloadDocumentQueryHandler(
            db, User(Guid.NewGuid()), Substitute.For<IBlobStorageService>());
        var act = () => sut.Handle(new DownloadDocumentQuery(doc.Id), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Download_allowed_for_admin()
    {
        using var db = CreateDb();
        var doc = Doc(Guid.NewGuid(), DocumentCategory.Transcript);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var storage = Substitute.For<IBlobStorageService>();
        storage.DownloadAsync(doc.StoragePath, Arg.Any<CancellationToken>())
            .Returns(Bytes());

        var sut = new DownloadDocumentQueryHandler(
            db, User(Guid.NewGuid(), "Admin"), storage);
        var result = await sut.Handle(new DownloadDocumentQuery(doc.Id), default);

        result.FileName.Should().Be(doc.FileName);
    }

    [Fact]
    public async Task Download_missing_document_throws_not_found()
    {
        using var db = CreateDb();
        var sut = new DownloadDocumentQueryHandler(
            db, User(Guid.NewGuid()), Substitute.For<IBlobStorageService>());

        var act = () => sut.Handle(new DownloadDocumentQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_soft_deletes_row_and_removes_bytes_for_owner()
    {
        using var db = CreateDb();
        var me = Guid.NewGuid();
        var doc = Doc(me, DocumentCategory.Transcript);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var storage = Substitute.For<IBlobStorageService>();
        var sut = new DeleteDocumentCommandHandler(
            db, User(me), storage, Clock(),
            NullLogger<DeleteDocumentCommandHandler>.Instance);

        await sut.Handle(new DeleteDocumentCommand(doc.Id), default);

        // The global query filter hides soft-deleted rows.
        (await db.Documents.AnyAsync()).Should().BeFalse();
        var raw = await db.Documents.IgnoreQueryFilters().SingleAsync();
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedByUserId.Should().Be(me);
        await storage.Received(1).DeleteAsync(doc.StoragePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_forbidden_for_non_owner()
    {
        using var db = CreateDb();
        var doc = Doc(Guid.NewGuid(), DocumentCategory.Transcript);
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var sut = new DeleteDocumentCommandHandler(
            db, User(Guid.NewGuid()), Substitute.For<IBlobStorageService>(), Clock(),
            NullLogger<DeleteDocumentCommandHandler>.Instance);

        var act = () => sut.Handle(new DeleteDocumentCommand(doc.Id), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private static Document Doc(Guid ownerId, DocumentCategory category) => new()
    {
        Id = Guid.NewGuid(),
        OwnerUserId = ownerId,
        FileName = "transcript.pdf",
        ContentType = "application/pdf",
        SizeBytes = 1024,
        StoragePath = $"local:documents/{Guid.NewGuid():N}/transcript.pdf",
        Category = category,
        UploadedAt = Now,
    };
}
