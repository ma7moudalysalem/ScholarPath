using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgradeRequest;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.UpgradeRequests;

public class SubmitConsultantUpgradeRequestCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService CurrentUser(Guid? id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static IDateTimeService Clock()
    {
        var c = Substitute.For<IDateTimeService>();
        c.UtcNow.Returns(DateTimeOffset.UtcNow);
        return c;
    }

    private static SubmitConsultantUpgradeRequestCommandHandler Sut(
        ApplicationDbContext db, Guid? userId, IUserAdministration admin,
        INotificationDispatcher? notifications = null) =>
        new(db, CurrentUser(userId), admin, Clock(),
            notifications ?? Substitute.For<INotificationDispatcher>(),
            NullLogger<SubmitConsultantUpgradeRequestCommandHandler>.Instance);

    private static Guid SeedActiveStudent(ApplicationDbContext db, bool withDocs = true)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Active",
            LastName = "Student",
            Email = "student@scholarpath.local",
            UserName = "student@scholarpath.local",
            AccountStatus = AccountStatus.Active,
        });
        // GAP-1 / FR-ONB-08 — the upgrade handler now requires the same 3 onboarding
        // verification documents a fresh Consultant onboarding does, so the happy-path
        // fixtures seed them. Pass withDocs:false to exercise the missing-docs guard.
        if (withDocs)
        {
            // FR-ONB-13 — seed the three mandatory Consultant document types.
            var types = new[]
            {
                OnboardingDocumentType.ConsultantIdentityProof,
                OnboardingDocumentType.ConsultantDegreeCertificate,
                OnboardingDocumentType.ConsultantCvResume,
            };
            for (var i = 0; i < types.Length; i++)
            {
                db.Documents.Add(new Document
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = id,
                    FileName = $"verification-{i}.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 1024,
                    StoragePath = $"documents/{id}/verification-{i}.pdf",
                    Category = DocumentCategory.OnboardingDocument,
                    OnboardingType = types[i],
                });
            }
        }
        return id;
    }

    private static SubmitConsultantUpgradeRequestCommand ValidCommand() => new(
        Biography: "5 years guiding scholarship applicants.",
        ProfessionalTitle: "Admissions Consultant",
        HighestDegree: "MSc Education",
        FieldOfExpertise: "Graduate admissions",
        YearsOfExperience: 5,
        SessionFeeUsd: 40m,
        SessionDurationMinutes: 60,
        ExpertiseTags: new[] { "SoP", "Interview Prep" },
        Languages: new[] { "English", "Arabic" },
        Country: "Egypt",
        Timezone: "Africa/Cairo",
        LinkedInUrl: "https://linkedin.com/in/example",
        PortfolioUrl: null);

    private static IUserAdministration Admin(params string[] roles)
    {
        var a = Substitute.For<IUserAdministration>();
        a.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(roles);
        return a;
    }

    [Fact]
    public async Task Active_student_can_submit_consultant_upgrade()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        await db.SaveChangesAsync();

        var id = await Sut(db, userId, Admin("Student")).Handle(ValidCommand(), default);

        id.Should().NotBe(Guid.Empty);
        var saved = await db.UpgradeRequests
            .Include(r => r.User)
            .FirstAsync(r => r.UserId == userId);
        saved.Target.Should().Be(UpgradeTarget.Consultant);
        saved.Status.Should().Be(UpgradeRequestStatus.Pending);
        saved.Reason.Should().NotBeNullOrEmpty();

        var user = await db.Users.Include(u => u.Profile).FirstAsync(u => u.Id == userId);
        user.Profile.Should().NotBeNull();
        user.Profile!.ProfessionalTitle.Should().Be("Admissions Consultant");
        user.Profile.SessionFeeUsd.Should().Be(40m);
        user.Profile.SessionDurationMinutes.Should().Be(60);
        user.Profile.Timezone.Should().Be("Africa/Cairo");
        user.Profile.ExpertiseTagsJson.Should().Contain("SoP");
        user.Profile.LanguagesJson.Should().Contain("Arabic");
        user.CountryOfResidence.Should().Be("Egypt");
    }

    [Fact]
    public async Task Submission_notifies_active_admins_with_upgrades_deep_link()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        var adminId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = adminId,
            FirstName = "Site",
            LastName = "Admin",
            Email = "admin@scholarpath.local",
            UserName = "admin@scholarpath.local",
            ActiveRole = "Admin",
            AccountStatus = AccountStatus.Active,
        });
        await db.SaveChangesAsync();

        var notifications = Substitute.For<INotificationDispatcher>();

        await Sut(db, userId, Admin("Student"), notifications).Handle(ValidCommand(), default);

        await notifications.Received(1).DispatchAsync(
            adminId,
            NotificationType.UpgradeRequestSubmitted,
            Arg.Any<NotificationParams>(),
            "/admin/upgrades",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_not_authenticated()
    {
        using var db = CreateDb();
        var act = () => Sut(db, userId: null, Admin()).Handle(ValidCommand(), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Non_student_cannot_submit()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        await db.SaveChangesAsync();

        // Mock returns ScholarshipProvider role; user is not a Student.
        var act = () => Sut(db, userId, Admin("ScholarshipProvider")).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Student*");
    }

    [Fact]
    public async Task Existing_consultant_cannot_submit()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        await db.SaveChangesAsync();

        var act = () => Sut(db, userId, Admin("Student", "Consultant")).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already a Consultant*");
    }

    [Fact]
    public async Task Duplicate_pending_request_is_blocked()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var act = () => Sut(db, userId, Admin("Student")).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*pending consultant upgrade request*");
    }

    [Fact]
    public async Task Pending_block_ignores_decided_or_soft_deleted_history()
    {
        using var db = CreateDb();
        var userId = SeedActiveStudent(db);
        // A rejected request and a soft-deleted pending request should NOT
        // count toward the duplicate check — the student must be able to retry.
        db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Rejected,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });
        db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Pending,
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var id = await Sut(db, userId, Admin("Student")).Handle(ValidCommand(), default);

        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Non_active_account_is_blocked()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Pending",
            LastName = "Student",
            Email = "p@scholarpath.local",
            UserName = "p@scholarpath.local",
            AccountStatus = AccountStatus.Suspended,
        });
        await db.SaveChangesAsync();

        var act = () => Sut(db, id, Admin("Student")).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*account must be active*");
    }

    [Fact]
    public async Task Blocked_when_verification_documents_missing()
    {
        // GAP-1 / FR-ONB-08 — an active Student with all profile fields but NO
        // onboarding verification documents must be rejected, matching the bar a
        // fresh Consultant onboarding enforces.
        using var db = CreateDb();
        var userId = SeedActiveStudent(db, withDocs: false);
        await db.SaveChangesAsync();

        var act = () => Sut(db, userId, Admin("Student")).Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*verification document*");
    }

    [Fact]
    public void Validator_rejects_missing_required_fields()
    {
        var v = new SubmitConsultantUpgradeRequestCommandValidator();

        var bare = new SubmitConsultantUpgradeRequestCommand(
            Biography: "",
            ProfessionalTitle: "",
            HighestDegree: "",
            FieldOfExpertise: "",
            YearsOfExperience: null,
            SessionFeeUsd: null,
            SessionDurationMinutes: null,
            ExpertiseTags: null,
            Languages: null,
            Country: "",
            Timezone: "");
        v.Validate(bare).IsValid.Should().BeFalse();

        var valid = new SubmitConsultantUpgradeRequestCommand(
            Biography: "Hello world",
            ProfessionalTitle: "Senior Consultant",
            HighestDegree: "MSc",
            FieldOfExpertise: "Admissions",
            YearsOfExperience: 3,
            SessionFeeUsd: 50m,
            SessionDurationMinutes: 60,
            ExpertiseTags: new[] { "SoP" },
            Languages: new[] { "English" },
            Country: "Egypt",
            Timezone: "Africa/Cairo");
        v.Validate(valid).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_invalid_session_duration()
    {
        var v = new SubmitConsultantUpgradeRequestCommandValidator();
        var bad = new SubmitConsultantUpgradeRequestCommand(
            Biography: "Hello",
            ProfessionalTitle: "Title",
            HighestDegree: "MSc",
            FieldOfExpertise: "Admissions",
            YearsOfExperience: 5,
            SessionFeeUsd: 50m,
            SessionDurationMinutes: 25, // not allowed
            ExpertiseTags: new[] { "SoP" },
            Languages: new[] { "English" },
            Country: "Egypt",
            Timezone: "Africa/Cairo");
        v.Validate(bad).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_invalid_urls()
    {
        var v = new SubmitConsultantUpgradeRequestCommandValidator();
        var bad = new SubmitConsultantUpgradeRequestCommand(
            Biography: "Hello",
            ProfessionalTitle: "Title",
            HighestDegree: "MSc",
            FieldOfExpertise: "Admissions",
            YearsOfExperience: 5,
            SessionFeeUsd: 50m,
            SessionDurationMinutes: 60,
            ExpertiseTags: new[] { "SoP" },
            Languages: new[] { "English" },
            Country: "Egypt",
            Timezone: "Africa/Cairo",
            LinkedInUrl: "not-a-url");
        v.Validate(bad).IsValid.Should().BeFalse();
    }
}
