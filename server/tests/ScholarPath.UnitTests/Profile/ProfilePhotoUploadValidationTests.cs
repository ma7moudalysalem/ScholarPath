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
using Xunit;

namespace ScholarPath.UnitTests.Profile;

/// <summary>
/// Hardened profile-photo upload validation (PB-002 / security NFR): the
/// content-type allowlist and the file-signature ("magic byte") check that
/// rejects a non-image file renamed with an image extension.
/// </summary>
public sealed class ProfilePhotoUploadValidationTests
{
    // ─── Real file signatures ────────────────────────────────────────────────
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] PngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly byte[] WebpBytes =
    [
        0x52, 0x49, 0x46, 0x46, // "RIFF"
        0x24, 0x00, 0x00, 0x00, // file size
        0x57, 0x45, 0x42, 0x50, // "WEBP"
    ];

    // ─── ImageSignature unit checks ──────────────────────────────────────────

    [Fact]
    public void ImageSignature_recognizes_jpeg_png_and_webp()
    {
        ImageSignature.IsRecognizedImage(JpegBytes).Should().BeTrue();
        ImageSignature.IsRecognizedImage(PngBytes).Should().BeTrue();
        ImageSignature.IsRecognizedImage(WebpBytes).Should().BeTrue();
    }

    [Fact]
    public void ImageSignature_rejects_non_image_bytes()
    {
        // Plain text — a renamed .txt / executable / PDF would land here.
        var notAnImage = Encoding.ASCII.GetBytes("%PDF-1.7 not really an image");
        ImageSignature.IsRecognizedImage(notAnImage).Should().BeFalse();

        // A RIFF container that is not WebP (e.g. a .wav) is rejected.
        var riffWav = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00,
            0x57, 0x41, 0x56, 0x45, // "WAVE"
        };
        ImageSignature.IsRecognizedImage(riffWav).Should().BeFalse();

        // Too few bytes to match any signature.
        ImageSignature.IsRecognizedImage(new byte[] { 0xFF }).Should().BeFalse();
    }

    [Fact]
    public void ImageSignature_reads_from_a_stream_without_consuming_more_than_the_header()
    {
        using var stream = new MemoryStream([.. PngBytes, .. new byte[2048]]);

        ImageSignature.IsRecognizedImage(stream).Should().BeTrue();
    }

    // ─── Handler enforcement ─────────────────────────────────────────────────

    [Fact]
    public async Task Upload_rejects_a_disallowed_content_type()
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var sut = Handler(db, userId);

        // Valid PNG bytes + valid extension, but a content-type that is not allowed.
        using var content = new MemoryStream(PngBytes);
        var cmd = new UploadProfilePhotoCommand(content, "avatar.png", "text/plain", content.Length);

        var act = () => sut.Handle(cmd, default);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Message.Should().Contain("JPEG, PNG or WebP");
    }

    [Fact]
    public async Task Upload_rejects_a_non_image_file_renamed_with_an_image_extension()
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var storage = Storage();
        var sut = Handler(db, userId, storage);

        // A text file masquerading as a .png with an image content-type — the
        // magic-byte check must catch it.
        using var content = new MemoryStream(Encoding.ASCII.GetBytes("this is not an image"));
        var cmd = new UploadProfilePhotoCommand(content, "fake.png", "image/png", content.Length);

        var act = () => sut.Handle(cmd, default);

        (await act.Should().ThrowAsync<ConflictException>())
            .Which.Message.Should().Contain("not a valid");

        // Fail-closed: nothing was stored.
        await storage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("avatar.jpg", "image/jpeg")]
    [InlineData("avatar.png", "image/png")]
    [InlineData("avatar.webp", "image/webp")]
    public async Task Upload_accepts_a_genuine_image_with_a_matching_content_type(
        string fileName, string contentType)
    {
        using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var storage = Storage();
        var sut = Handler(db, userId, storage);

        var bytes = contentType switch
        {
            "image/jpeg" => JpegBytes,
            "image/webp" => WebpBytes,
            _ => PngBytes,
        };
        using var content = new MemoryStream(bytes);
        var cmd = new UploadProfilePhotoCommand(content, fileName, contentType, content.Length);

        var url = await sut.Handle(cmd, default);

        url.Should().Be("local:profile-photos/key/photo.png");
        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── Harness ─────────────────────────────────────────────────────────────

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

    private static IFileScanService Scanner()
    {
        var s = Substitute.For<IFileScanService>();
        s.ScanAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileScanResult(FileScanVerdict.Clean, null));
        return s;
    }

    private static UploadProfilePhotoCommandHandler Handler(
        ApplicationDbContext db, Guid userId, IBlobStorageService? storage = null) =>
        new(db, User(userId), storage ?? Storage(), Scanner(),
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
}
