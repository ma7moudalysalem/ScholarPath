using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Applications.Commands.SaveApplicationDraft;
using ScholarPath.Application.Applications.Commands.StartApplication;
using ScholarPath.Application.Applications.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Applications;

public class CreateApplicationValidatorTests
{
    private readonly StartApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ScholarshipId_Is_Empty()
    {
        var command = new StartApplicationCommand(Guid.Empty, "Some notes");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ScholarshipId);
    }

    [Fact]
    public void Should_Have_Error_When_PersonalNotes_Exceed_Maximum_Length()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), new string('a', 4001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PersonalNotes_Is_Null()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PersonalNotes_Is_Exactly_4000_Chars()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), new string('a', 4000));
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalNotes);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class ApplicationStateMachineTests
{
    // ── Allowed transitions ───────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Withdrawn)]
    public void IsTransitionAllowed_Should_Return_True_For_Valid_Transitions(
        ApplicationStatus from, ApplicationStatus to)
    {
        ApplicationStateMachine.IsTransitionAllowed(from, to).Should().BeTrue();
    }

    // ── Disallowed transitions ────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Accepted, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Withdrawn, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Accepted, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Accepted)]
    public void IsTransitionAllowed_Should_Return_False_For_Invalid_Transitions(
        ApplicationStatus from, ApplicationStatus to)
    {
        ApplicationStateMachine.IsTransitionAllowed(from, to).Should().BeFalse();
    }

    [Fact]
    public void EnsureTransition_Should_Throw_ConflictException_For_Invalid_Transition()
    {
        var act = () => ApplicationStateMachine.EnsureTransition(
            ApplicationStatus.Accepted,
            ApplicationStatus.Withdrawn);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void EnsureTransition_Should_Not_Throw_For_Valid_Transition()
    {
        var act = () => ApplicationStateMachine.EnsureTransition(
            ApplicationStatus.Draft,
            ApplicationStatus.Pending);

        act.Should().NotThrow();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class ApplicationTrackerIsActiveTests
{
    // ── Active statuses ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Shortlisted)]
    public void IsActive_Should_Be_True_For_Non_Terminal_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsActive.Should().BeTrue();
    }

    // ── Terminal statuses ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Accepted)]
    public void IsActive_Should_Be_False_For_Terminal_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsActive.Should().BeFalse();
    }

    // ── IsReadOnly ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void IsReadOnly_Should_Be_True_For_Final_Decision_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsReadOnly.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Shortlisted)]
    public void IsReadOnly_Should_Be_False_For_Non_Final_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsReadOnly.Should().BeFalse();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Covers <see cref="StartApplicationCommandHandler"/> — in particular the
/// idempotent-resume behaviour: a repeated "Apply" on a scholarship the student
/// already has a live application for must reopen it, not dead-end on a 409.
/// </summary>
public sealed class StartApplicationCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _studentId = Guid.NewGuid();

    public StartApplicationCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _currentUser.UserId.Returns(_studentId);

        // A student whose profile is complete enough to apply (TC-003 gate).
        _db.Users.Add(new ApplicationUser
        {
            Id = _studentId,
            FirstName = "Test",
            LastName = "Student",
            Email = "student@test.local",
            UserName = "student@test.local",
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = _studentId,
                AcademicLevel = AcademicLevel.Undergrad,
                FieldOfStudy = "Computer Science",
            },
        });
        _db.SaveChanges();
    }

    private Guid SeedOpenScholarship()
    {
        var id = Guid.NewGuid();
        _db.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"test-{id:N}",
            Mode = ListingMode.InApp,
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
        });
        _db.SaveChanges();
        return id;
    }

    private ApplicationTracker SeedApplication(Guid scholarshipId, ApplicationStatus status)
    {
        var app = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = _studentId,
            ScholarshipId = scholarshipId,
            Mode = ApplicationMode.InApp,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        };
        _db.Applications.Add(app);
        _db.SaveChanges();
        return app;
    }

    private StartApplicationCommandHandler NewHandler() => new(_db, _currentUser);

    [Fact]
    public async Task Handle_NoExistingApplication_CreatesADraftApplication()
    {
        var scholarshipId = SeedOpenScholarship();

        var result = await NewHandler().Handle(
            new StartApplicationCommand(scholarshipId, null), CancellationToken.None);

        result.AlreadyExisted.Should().BeFalse();
        var apps = await _db.Applications.ToListAsync();
        apps.Should().ContainSingle();
        apps[0].Id.Should().Be(result.ApplicationId);
        apps[0].Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public async Task Handle_ActiveDraftAlreadyExists_ResumesItWithoutCreatingADuplicate()
    {
        var scholarshipId = SeedOpenScholarship();
        var existing = SeedApplication(scholarshipId, ApplicationStatus.Draft);

        var result = await NewHandler().Handle(
            new StartApplicationCommand(scholarshipId, null), CancellationToken.None);

        result.AlreadyExisted.Should().BeTrue();
        result.ApplicationId.Should().Be(existing.Id);
        (await _db.Applications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_SubmittedApplicationAlreadyExists_ResumesItRatherThanErroring()
    {
        var scholarshipId = SeedOpenScholarship();
        var existing = SeedApplication(scholarshipId, ApplicationStatus.Pending);

        var result = await NewHandler().Handle(
            new StartApplicationCommand(scholarshipId, null), CancellationToken.None);

        result.AlreadyExisted.Should().BeTrue();
        result.ApplicationId.Should().Be(existing.Id);
    }

    [Fact]
    public async Task Handle_OnlyATerminalApplicationExists_CreatesAFreshDraft()
    {
        var scholarshipId = SeedOpenScholarship();
        SeedApplication(scholarshipId, ApplicationStatus.Withdrawn);

        var result = await NewHandler().Handle(
            new StartApplicationCommand(scholarshipId, null), CancellationToken.None);

        // A withdrawn application is terminal — re-applying must be allowed.
        result.AlreadyExisted.Should().BeFalse();
        (await _db.Applications.CountAsync()).Should().Be(2);
    }

    // BUG-01 (FR-043/044): a scholarship whose deadline has passed must reject a
    // new application even while Status is still Open — the auto-close job flips
    // Status only on a schedule, so there is a window the deadline guards.
    [Fact]
    public async Task Handle_ScholarshipDeadlineHasPassed_ThrowsConflict()
    {
        var id = Guid.NewGuid();
        _db.Scholarships.Add(new Scholarship
        {
            Id = id,
            TitleEn = "Past",
            TitleAr = "منتهية",
            DescriptionEn = "d",
            DescriptionAr = "و",
            Slug = $"past-{id:N}",
            Mode = ListingMode.InApp,
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await _db.SaveChangesAsync();

        var act = () => NewHandler().Handle(
            new StartApplicationCommand(id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*deadline*");
    }

    public void Dispose() => _db.Dispose();
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Covers <see cref="SaveApplicationDraftCommandHandler"/> — persisting the
/// student's in-progress form answers, and the guards that keep a submitted
/// application read-only and a draft editable only by its owner.
/// </summary>
public sealed class SaveApplicationDraftCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _studentId = Guid.NewGuid();

    public SaveApplicationDraftCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _currentUser.UserId.Returns(_studentId);
    }

    private ApplicationTracker SeedApplication(ApplicationStatus status, Guid? studentId = null)
    {
        var app = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = studentId ?? _studentId,
            ScholarshipId = Guid.NewGuid(),
            Mode = ApplicationMode.InApp,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _db.Applications.Add(app);
        _db.SaveChanges();
        return app;
    }

    private SaveApplicationDraftCommandHandler NewHandler() => new(_db, _currentUser);

    [Fact]
    public async Task Handle_DraftOwnedByCaller_PersistsTheAnswers()
    {
        var app = SeedApplication(ApplicationStatus.Draft);

        await NewHandler().Handle(
            new SaveApplicationDraftCommand(app.Id, """{"gpa":3.9}""", """["cv.pdf"]""", "my notes"),
            CancellationToken.None);

        var saved = await _db.Applications.FindAsync(app.Id);
        saved!.FormDataJson.Should().Be("""{"gpa":3.9}""");
        saved.AttachedDocumentsJson.Should().Be("""["cv.pdf"]""");
        saved.PersonalNotes.Should().Be("my notes");
    }

    [Fact]
    public async Task Handle_AlreadySubmittedApplication_ThrowsConflict()
    {
        var app = SeedApplication(ApplicationStatus.Pending);

        var act = () => NewHandler().Handle(
            new SaveApplicationDraftCommand(app.Id, "{}", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DraftOwnedByAnotherStudent_ThrowsNotFound()
    {
        var app = SeedApplication(ApplicationStatus.Draft, studentId: Guid.NewGuid());

        var act = () => NewHandler().Handle(
            new SaveApplicationDraftCommand(app.Id, "{}", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
