using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ApproveOnboarding;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Admin;

public class ReviewOnboardingRoleGrantTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Guid SeedPendingUser(ApplicationDbContext db, string requestedRole)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Pending",
            LastName = "Payee",
            Email = $"{id:N}@scholarpath.local",
            UserName = $"{id:N}@scholarpath.local",
            AccountStatus = AccountStatus.PendingApproval,
            ActiveRole = requestedRole,
        });
        // FR-ONB-13 — approval now requires the role's mandatory document types to be
        // present, so seed them for the pending applicant.
        var types = requestedRole == "Consultant"
            ? new[]
            {
                OnboardingDocumentType.ConsultantIdentityProof,
                OnboardingDocumentType.ConsultantDegreeCertificate,
                OnboardingDocumentType.ConsultantCvResume,
            }
            : new[]
            {
                OnboardingDocumentType.ProviderLegalRegistration,
                OnboardingDocumentType.ProviderAuthorizedRepresentativeId,
            };
        foreach (var type in types)
        {
            db.Documents.Add(new Document
            {
                Id = Guid.NewGuid(),
                OwnerUserId = id,
                FileName = $"{type}.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1024,
                StoragePath = $"documents/{id}/{type}.pdf",
                Category = DocumentCategory.OnboardingDocument,
                OnboardingType = type,
            });
        }
        return id;
    }

    private static ReviewOnboardingCommandHandler Sut(
        ApplicationDbContext db, IUserAdministration admin) =>
        new(db, admin, Substitute.For<INotificationDispatcher>(),
            NullLogger<ReviewOnboardingCommandHandler>.Instance);

    [Fact]
    public async Task Approval_grants_the_requested_role_and_completes_onboarding()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "ScholarshipProvider");
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.SetAccountStatusAsync(userId, AccountStatus.Active,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Approve, null), default);

        await admin.Received().AddRoleAsync(userId, "ScholarshipProvider", Arg.Any<CancellationToken>());
        (await db.Users.FirstAsync(u => u.Id == userId)).IsOnboardingComplete.Should().BeTrue();
    }

    [Fact]
    public async Task Rejection_does_not_grant_a_role()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "Consultant");
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        // A rejected applicant returns to Unassigned so they can resubmit (FR-152).
        admin.SetAccountStatusAsync(userId, AccountStatus.Unassigned,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Reject, "Not verified"), default);

        await admin.DidNotReceive()
            .AddRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // AUTH-CODE-06 — rejection reason must be persisted on UserProfile so the
    // onboarding wizard can render it when the applicant resubmits.
    [Fact]
    public async Task Rejection_persists_the_reviewer_notes_on_profile()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "ScholarshipProvider");
        db.UserProfiles.Add(new UserProfile { UserId = userId });
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.SetAccountStatusAsync(userId, AccountStatus.Unassigned,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Reject,
                "Registration certificate unreadable. Please upload a clearer scan."),
            default);

        var profile = await db.UserProfiles.FirstAsync(p => p.UserId == userId);
        profile.LastOnboardingRejectionReason
            .Should().Be("Registration certificate unreadable. Please upload a clearer scan.");
        profile.LastOnboardingRejectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Approval_clears_any_stale_rejection_reason()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "ScholarshipProvider");
        db.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            LastOnboardingRejectionReason = "Previous rejection",
            LastOnboardingRejectedAt = DateTimeOffset.UtcNow.AddDays(-3),
        });
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.SetAccountStatusAsync(userId, AccountStatus.Active,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Approve, null), default);

        var profile = await db.UserProfiles.FirstAsync(p => p.UserId == userId);
        profile.LastOnboardingRejectionReason.Should().BeNull();
        profile.LastOnboardingRejectedAt.Should().BeNull();
    }

    [Fact]
    public async Task Approval_is_blocked_when_required_document_types_are_missing()
    {
        // FR-ONB-13 approval gate — a pending provider with no verification documents
        // cannot be approved.
        using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "No",
            LastName = "Docs",
            Email = $"{id:N}@scholarpath.local",
            UserName = $"{id:N}@scholarpath.local",
            AccountStatus = AccountStatus.PendingApproval,
            ActiveRole = "ScholarshipProvider",
        });
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        var act = () => Sut(db, admin).Handle(
            new ReviewOnboardingCommand(id, OnboardingDecision.Approve, null), default);

        await act.Should().ThrowAsync<ScholarPath.Application.Common.Exceptions.ConflictException>()
            .WithMessage("*missing required*");
    }
}
