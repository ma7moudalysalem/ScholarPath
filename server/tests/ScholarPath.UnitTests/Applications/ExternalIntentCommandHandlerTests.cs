using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.Commands.ExternalIntent;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Applications;

public sealed class ExternalIntentCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ExternalIntentCommandHandler _handler;

    public ExternalIntentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new ExternalIntentCommandHandler(_db, _currentUser);
    }

    // Seeds a scholarship with the requested listing mode.
    private async Task<Guid> SeedScholarshipAsync(ListingMode mode)
    {
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerCompanyId = Guid.NewGuid(),
            TitleEn = "External Scholarship",
            TitleAr = "منحة خارجية",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"external-scholarship-{Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            Mode = mode,
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
        };
        _db.Scholarships.Add(scholarship);
        await _db.SaveChangesAsync();
        return scholarship.Id;
    }

    [Fact]
    public async Task Handle_ValidExternalListing_CreatesIntendingApplication()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var scholarshipId = await SeedScholarshipAsync(ListingMode.ExternalUrl);

        var command = new ExternalIntentCommand(
            scholarshipId,
            "https://provider.example.com/apply",
            "REF-123",
            "Need to upload transcript by Friday.");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == result);
        app.Should().NotBeNull();
        app!.StudentId.Should().Be(studentId);
        app.ScholarshipId.Should().Be(scholarshipId);
        app.Mode.Should().Be(ApplicationMode.External);
        app.Status.Should().Be(ApplicationStatus.Intending);
        app.ExternalTrackingUrl.Should().Be("https://provider.example.com/apply");
        app.ExternalReferenceId.Should().Be("REF-123");
        app.PersonalNotes.Should().Be("Need to upload transcript by Friday.");
    }

    [Fact]
    public async Task Handle_ScholarshipNotFound_ThrowsNotFound()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());

        var command = new ExternalIntentCommand(Guid.NewGuid(), null, null, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InAppListing_ThrowsConflict()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        var scholarshipId = await SeedScholarshipAsync(ListingMode.InApp);

        var command = new ExternalIntentCommand(scholarshipId, null, null, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DuplicateActiveApplication_ThrowsConflict()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var scholarshipId = await SeedScholarshipAsync(ListingMode.ExternalUrl);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ScholarshipId = scholarshipId,
            Mode = ApplicationMode.External,
            Status = ApplicationStatus.Intending,
        });
        await _db.SaveChangesAsync();

        var command = new ExternalIntentCommand(scholarshipId, null, null, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_PriorTerminalApplication_AllowsReapplication()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var scholarshipId = await SeedScholarshipAsync(ListingMode.ExternalUrl);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ScholarshipId = scholarshipId,
            Mode = ApplicationMode.External,
            Status = ApplicationStatus.Rejected,
        });
        await _db.SaveChangesAsync();

        var command = new ExternalIntentCommand(scholarshipId, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    public void Dispose() => _db.Dispose();
}

// ─────────────────────────────────────────────────────────────────────────────

public class ExternalIntentValidatorTests
{
    private readonly ExternalIntentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ScholarshipId_Is_Empty()
    {
        var command = new ExternalIntentCommand(Guid.Empty, null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ScholarshipId);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://provider.example.com/apply")]
    [InlineData("/relative/path")]
    public void Should_Have_Error_When_TrackingUrl_Is_Not_A_Valid_Http_Url(string url)
    {
        var command = new ExternalIntentCommand(Guid.NewGuid(), url, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExternalTrackingUrl);
    }

    [Theory]
    [InlineData("https://provider.example.com/apply")]
    [InlineData("http://provider.example.com/apply")]
    public void Should_Not_Have_Error_When_TrackingUrl_Is_A_Valid_Http_Url(string url)
    {
        var command = new ExternalIntentCommand(Guid.NewGuid(), url, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExternalTrackingUrl);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Optional_Fields_Are_Null()
    {
        var command = new ExternalIntentCommand(Guid.NewGuid(), null, null, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ExternalTrackingUrl);
        result.ShouldNotHaveValidationErrorFor(x => x.ExternalReferenceId);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Have_Error_When_PersonalNotes_Exceed_Maximum_Length()
    {
        var command = new ExternalIntentCommand(Guid.NewGuid(), null, null, new string('a', 4001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Have_Error_When_ReferenceId_Exceeds_Maximum_Length()
    {
        var command = new ExternalIntentCommand(Guid.NewGuid(), null, new string('a', 201), null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ExternalReferenceId);
    }
}
