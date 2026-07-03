using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.SelectRole;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

public class SelectRoleCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService CurrentUser(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static ITokenService TokenService()
    {
        var ts = Substitute.For<ITokenService>();
        ts.IssueTokens(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(new TokenPair("access", "refresh",
                DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)));
        return ts;
    }

    private static SelectRoleCommandHandler Sut(
        ApplicationDbContext db, Guid userId, IUserAdministration admin,
        INotificationDispatcher? notifications = null) =>
        new(db, CurrentUser(userId), admin, TokenService(),
            notifications ?? Substitute.For<INotificationDispatcher>(),
            NullLogger<SelectRoleCommandHandler>.Instance);

    private static Guid SeedUnassignedUser(ApplicationDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "New",
            LastName = "User",
            Email = "new@scholarpath.local",
            UserName = "new@scholarpath.local",
            AccountStatus = AccountStatus.Unassigned,
        });
        return id;
    }

    /// <summary>
    /// Seeds the mandatory onboarding verification documents the handler now
    /// requires before a ScholarshipProvider/Consultant can enter the admin queue
    /// (AUTH-CODE-02). Defaults to the maximum so callers can simply attach the
    /// required minimum for either role.
    /// </summary>
    private static void SeedOnboardingDocuments(ApplicationDbContext db, Guid userId, int count = 3)
    {
        // FR-ONB-13 — seed one document per mandatory type for BOTH roles so whichever
        // role a test selects clears the required-type check. (count retained for
        // call-site compatibility; the type set is what the handler now validates.)
        var types = new[]
        {
            OnboardingDocumentType.ProviderLegalRegistration,
            OnboardingDocumentType.ProviderAuthorizedRepresentativeId,
            OnboardingDocumentType.ConsultantIdentityProof,
            OnboardingDocumentType.ConsultantDegreeCertificate,
            OnboardingDocumentType.ConsultantCvResume,
        };
        for (var i = 0; i < types.Length; i++)
        {
            db.Documents.Add(new Document
            {
                Id = Guid.NewGuid(),
                OwnerUserId = userId,
                FileName = $"verification-{i}.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1024,
                StoragePath = $"documents/{userId}/verification-{i}.pdf",
                Category = DocumentCategory.OnboardingDocument,
                OnboardingType = types[i],
                UploadedAt = DateTimeOffset.UtcNow,
            });
        }
    }

    /// <summary>Seeds a single onboarding document tagged with a specific type.</summary>
    private static void SeedTypedOnboardingDoc(ApplicationDbContext db, Guid userId, OnboardingDocumentType type)
    {
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            FileName = $"{type}.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            StoragePath = $"documents/{userId}/{type}.pdf",
            Category = DocumentCategory.OnboardingDocument,
            OnboardingType = type,
            UploadedAt = DateTimeOffset.UtcNow,
        });
    }

    [Fact]
    public async Task Student_selection_activates_account_immediately()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        await Sut(db, userId, admin).Handle(new SelectRoleCommand("Student"), default);

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.AccountStatus.Should().Be(AccountStatus.Active);
        user.ActiveRole.Should().Be("Student");
        user.IsOnboardingComplete.Should().BeTrue();
        await admin.Received().AddRoleAsync(userId, "Student", Arg.Any<CancellationToken>());
    }

    private static OnboardingDetails ValidScholarshipProviderDetails() => new(
        OrganizationLegalName: "Acme University",
        OrganizationWebsite: "https://acme.edu",
        OrganizationEmail: "admissions@acme.edu",
        OrganizationCountry: "Egypt",
        ScholarshipProviderType: "University",
        ScholarshipProviderDescription: "A research-led university.",
        OrganizationRegistrationNumber: "ACME-1234",
        OrganizationTaxNumber: null,
        ContactPersonFullName: "Hala Mostafa",
        ContactPersonPosition: "Admissions Director",
        ContactPhoneNumber: "+201234567890");

    private static OnboardingDetails ValidConsultantDetails() => new(
        Biography: "10 years guiding scholarship applicants.",
        ProfessionalTitle: "Senior Admissions Consultant",
        HighestDegree: "MSc Education",
        FieldOfExpertise: "Graduate admissions",
        YearsOfExperience: 10,
        SessionFeeUsd: 60m,
        SessionDurationMinutes: 60,
        ExpertiseTags: new[] { "SoP", "Interview Prep" },
        Languages: new[] { "English", "Arabic" },
        Country: "Egypt",
        Timezone: "Africa/Cairo",
        LinkedInUrl: "https://linkedin.com/in/example",
        PortfolioUrl: null);

    [Fact]
    public async Task ScholarshipProvider_selection_enters_the_onboarding_queue()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        SeedOnboardingDocuments(db, userId, count: 2); // AUTH-CODE-02 — ScholarshipProvider needs >= 2 docs.
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        await Sut(db, userId, admin).Handle(
            new SelectRoleCommand("ScholarshipProvider", ValidScholarshipProviderDetails()), default);

        var user = await db.Users.Include(u => u.Profile).FirstAsync(u => u.Id == userId);
        user.AccountStatus.Should().Be(AccountStatus.PendingApproval);
        user.ActiveRole.Should().Be("ScholarshipProvider");
        user.IsOnboardingComplete.Should().BeFalse();
        user.Profile.Should().NotBeNull();
        user.Profile!.OrganizationLegalName.Should().Be("Acme University");
        user.Profile.OrganizationEmail.Should().Be("admissions@acme.edu");
        user.Profile.OrganizationCountry.Should().Be("Egypt");
        user.Profile.ScholarshipProviderType.Should().Be("University");
        user.Profile.ContactPersonFullName.Should().Be("Hala Mostafa");
        user.Profile.ContactPhoneNumber.Should().Be("+201234567890");
        await admin.DidNotReceive()
            .AddRoleAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScholarshipProvider_submission_notifies_active_admins_with_onboarding_deep_link()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        SeedOnboardingDocuments(db, userId, count: 2);
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

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());
        var notifications = Substitute.For<INotificationDispatcher>();

        await Sut(db, userId, admin, notifications).Handle(
            new SelectRoleCommand("ScholarshipProvider", ValidScholarshipProviderDetails()), default);

        await notifications.Received(1).DispatchAsync(
            adminId,
            NotificationType.OnboardingSubmitted,
            Arg.Any<NotificationParams>(),
            "/admin/onboarding",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Student_selection_does_not_notify_admins()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Site",
            LastName = "Admin",
            Email = "admin2@scholarpath.local",
            UserName = "admin2@scholarpath.local",
            ActiveRole = "Admin",
            AccountStatus = AccountStatus.Active,
        });
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());
        var notifications = Substitute.For<INotificationDispatcher>();

        await Sut(db, userId, admin, notifications).Handle(new SelectRoleCommand("Student"), default);

        await notifications.DidNotReceive().DispatchAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consultant_selection_persists_the_extended_profile()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        SeedOnboardingDocuments(db, userId, count: 3); // AUTH-CODE-02 — Consultant needs >= 3 docs.
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        await Sut(db, userId, admin).Handle(
            new SelectRoleCommand("Consultant", ValidConsultantDetails()), default);

        var user = await db.Users.Include(u => u.Profile).FirstAsync(u => u.Id == userId);
        user.AccountStatus.Should().Be(AccountStatus.PendingApproval);
        user.Profile.Should().NotBeNull();
        user.Profile!.ProfessionalTitle.Should().Be("Senior Admissions Consultant");
        user.Profile.HighestDegree.Should().Be("MSc Education");
        user.Profile.FieldOfExpertise.Should().Be("Graduate admissions");
        user.Profile.YearsOfExperience.Should().Be(10);
        user.Profile.SessionFeeUsd.Should().Be(60m);
        user.Profile.SessionDurationMinutes.Should().Be(60);
        user.Profile.Timezone.Should().Be("Africa/Cairo");
        user.Profile.LinkedInUrl.Should().Be("https://linkedin.com/in/example");
        user.Profile.ExpertiseTagsJson.Should().Contain("SoP");
        user.Profile.LanguagesJson.Should().Contain("Arabic");
        user.CountryOfResidence.Should().Be("Egypt");
    }

    [Fact]
    public async Task Rejects_a_second_role_selection()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(new[] { "Student" });

        var act = () => Sut(db, userId, admin).Handle(new SelectRoleCommand("ScholarshipProvider"), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public void Validator_rejects_unknown_role()
    {
        var v = new SelectRoleCommandValidator();

        v.Validate(new SelectRoleCommand("Admin")).IsValid.Should().BeFalse();
        v.Validate(new SelectRoleCommand("")).IsValid.Should().BeFalse();
        v.Validate(new SelectRoleCommand("Student")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_requires_all_company_onboarding_fields()
    {
        var v = new SelectRoleCommandValidator();

        // Bare ScholarshipProvider role with no details — must fail.
        v.Validate(new SelectRoleCommand("ScholarshipProvider")).IsValid.Should().BeFalse();

        // Details with most fields missing — must fail.
        var partial = new SelectRoleCommand("ScholarshipProvider", new OnboardingDetails(
            OrganizationLegalName: "Acme"));
        v.Validate(partial).IsValid.Should().BeFalse();

        // Full ScholarshipProvider details — must pass.
        v.Validate(new SelectRoleCommand("ScholarshipProvider", ValidScholarshipProviderDetails())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_requires_all_consultant_onboarding_fields()
    {
        var v = new SelectRoleCommandValidator();

        v.Validate(new SelectRoleCommand("Consultant")).IsValid.Should().BeFalse();

        var partial = new SelectRoleCommand("Consultant", new OnboardingDetails(
            Biography: "Short bio without all the required extras."));
        v.Validate(partial).IsValid.Should().BeFalse();

        v.Validate(new SelectRoleCommand("Consultant", ValidConsultantDetails())).IsValid
            .Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_invalid_company_type()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidScholarshipProviderDetails() with { ScholarshipProviderType = "Bogus" };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_rejects_invalid_session_duration()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidConsultantDetails() with { SessionDurationMinutes = 25 };
        v.Validate(new SelectRoleCommand("Consultant", d)).IsValid.Should().BeFalse();
    }

    // ── AUTH-CODE-02 — mandatory verification documents ──────────────────────

    [Fact]
    public async Task ScholarshipProvider_submission_blocked_when_no_verification_documents()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        var act = () => Sut(db, userId, admin).Handle(
            new SelectRoleCommand("ScholarshipProvider", ValidScholarshipProviderDetails()), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*verification document*");
    }

    [Fact]
    public async Task Consultant_submission_blocked_when_too_few_documents()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        // FR-ONB-13 — only 2 of the 3 required Consultant types (missing the CV).
        SeedTypedOnboardingDoc(db, userId, OnboardingDocumentType.ConsultantIdentityProof);
        SeedTypedOnboardingDoc(db, userId, OnboardingDocumentType.ConsultantDegreeCertificate);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        var act = () => Sut(db, userId, admin).Handle(
            new SelectRoleCommand("Consultant", ValidConsultantDetails()), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*verification document*");
    }

    // ── AUTH-CODE-03 — conditional applicability fields ──────────────────────

    [Fact]
    public void Validator_requires_tax_reason_when_not_tax_registered()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidScholarshipProviderDetails() with { IsTaxRegistered = false, TaxNotApplicableReason = null };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeFalse();

        var ok = d with { TaxNotApplicableReason = "Charity is not tax-registered in Egypt." };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", ok)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_requires_tax_number_when_tax_registered()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidScholarshipProviderDetails() with { IsTaxRegistered = true, OrganizationTaxNumber = null };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_requires_legal_reason_when_not_legally_registered()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidScholarshipProviderDetails() with { IsLegallyRegistered = false, LegalRegistrationNotApplicableReason = null };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeFalse();
    }

    // ── AUTH-CODE-04 — validator rule alignment ──────────────────────────────

    [Fact]
    public void Validator_rejects_invalid_website_url()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidScholarshipProviderDetails() with { OrganizationWebsite = "acme.edu" }; // missing scheme
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_extended_company_description_up_to_2000_chars()
    {
        var v = new SelectRoleCommandValidator();
        // 1500 chars: between the old 1000 cap (rejected before) and the new 2000 cap.
        var d = ValidScholarshipProviderDetails() with { ScholarshipProviderDescription = new string('A', 1500) };
        v.Validate(new SelectRoleCommand("ScholarshipProvider", d)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_zero_years_of_experience()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidConsultantDetails() with { YearsOfExperience = 0 };
        v.Validate(new SelectRoleCommand("Consultant", d)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_120_minute_session_duration()
    {
        var v = new SelectRoleCommandValidator();
        var d = ValidConsultantDetails() with { SessionDurationMinutes = 120 };
        v.Validate(new SelectRoleCommand("Consultant", d)).IsValid.Should().BeTrue();
    }
}
