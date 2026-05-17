using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Profile.Commands.UploadProfilePhoto;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Security;

/// <summary>
/// Antivirus scanning of uploaded files (security NFR): the NoOp scanner used
/// in dev / tests, and fail-closed enforcement in the profile-photo upload
/// path. The document-vault path's enforcement is covered in
/// <c>DocumentVaultTests</c>.
/// </summary>
public sealed class FileScanningTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService User(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static IBlobStorageService Storage()
    {
        var s = Substitute.For<IBlobStorageService>();
        s.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("local:profile-photos/key/photo.png");
        return s;
    }

    private static IFileScanService Scanner(
        FileScanVerdict verdict = FileScanVerdict.Clean, string? detail = null)
    {
        var s = Substitute.For<IFileScanService>();
        s.ScanAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileScanResult(verdict, detail));
        return s;
    }

    // A minimal byte buffer that begins with a valid PNG signature, so the
    // profile-photo upload's magic-byte check accepts it and the test then
    // exercises the antivirus-scan logic these tests are about.
    private static MemoryStream Bytes() => new(
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        .. Encoding.UTF8.GetBytes("image-bytes"),
    ]);

    private static UploadProfilePhotoCommand PhotoCmd(Stream content) =>
        new(content, "avatar.png", "image/png", content.Length);

    private static UploadProfilePhotoCommandHandler PhotoHandler(
        ApplicationDbContext db, Guid userId, IBlobStorageService storage, IFileScanService scanner) =>
        new(db, User(userId), storage, scanner,
            NullLogger<UploadProfilePhotoCommandHandler>.Instance);

    private static async Task<Guid> SeedUserAsync(ApplicationDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            Email = "u@test.local",
            NormalizedEmail = "U@TEST.LOCAL",
            UserName = "u@test.local",
            NormalizedUserName = "U@TEST.LOCAL",
            FirstName = "Test",
            LastName = "User",
        });
        await db.SaveChangesAsync();
        return id;
    }

    // ─── NoOpFileScanService ─────────────────────────────────────────────────

    [Fact]
    public async Task NoOpFileScanService_always_returns_clean()
    {
        var sut = new NoOpFileScanService(NullLogger<NoOpFileScanService>.Instance);

        using var content = Bytes();
        var result = await sut.ScanAsync(content, "anything.bin", default);

        result.Verdict.Should().Be(FileScanVerdict.Clean);
        result.IsClean.Should().BeTrue();
        result.Detail.Should().BeNull();
    }

    [Fact]
    public void FileScanResult_IsClean_is_true_only_for_a_clean_verdict()
    {
        new FileScanResult(FileScanVerdict.Clean, null).IsClean.Should().BeTrue();
        new FileScanResult(FileScanVerdict.Infected, "X").IsClean.Should().BeFalse();
        new FileScanResult(FileScanVerdict.ScanUnavailable, "X").IsClean.Should().BeFalse();
    }

    // ─── Profile-photo upload: fail-closed enforcement ───────────────────────

    [Fact]
    public async Task Profile_photo_upload_rejects_when_scanner_finds_malware()
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var storage = Storage();
        var sut = PhotoHandler(db, userId, storage,
            Scanner(FileScanVerdict.Infected, "Eicar-Test-Signature"));

        using var content = Bytes();
        var act = () => sut.Handle(PhotoCmd(content), default);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Message.Should().Contain("malware detected");

        // Fail-closed: nothing stored.
        await storage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Profile_photo_upload_rejects_when_scan_unavailable()
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var storage = Storage();
        var sut = PhotoHandler(db, userId, storage,
            Scanner(FileScanVerdict.ScanUnavailable, "clamd unreachable"));

        using var content = Bytes();
        var act = () => sut.Handle(PhotoCmd(content), default);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Message.Should().Contain("could not be virus-scanned");

        await storage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Profile_photo_upload_stores_file_when_scan_is_clean()
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var storage = Storage();
        var sut = PhotoHandler(db, userId, storage, Scanner(FileScanVerdict.Clean));

        using var content = Bytes();
        var url = await sut.Handle(PhotoCmd(content), default);

        url.Should().Be("local:profile-photos/key/photo.png");
        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
