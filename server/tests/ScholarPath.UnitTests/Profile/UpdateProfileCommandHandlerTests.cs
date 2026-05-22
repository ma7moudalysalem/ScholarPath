using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Profile;

/// <summary>
/// Profile-update handler behaviour: company re-verification trigger
/// (CR-PROF-07), mass-assignment defence (CR-PROF-11), and consultant
/// professional-field persistence (CR-PROF-08).
/// </summary>
public sealed class UpdateProfileCommandHandlerTests
{
    private static UpdateProfileRequestDto Empty() => new(
        FirstName: null, LastName: null, CountryOfResidence: null, PreferredLanguage: null,
        Biography: null, DateOfBirth: null, Nationality: null, LinkedInUrl: null, WebsiteUrl: null,
        AcademicLevel: null, FieldOfStudy: null, CurrentInstitution: null, Gpa: null, GpaScale: null,
        OrganizationLegalName: null, OrganizationWebsite: null, SessionFeeUsd: null,
        SessionDurationMinutes: null, ProfessionalTitle: null, YearsOfExperience: null,
        ExpertiseTags: null, Languages: null, Timezone: null,
        PreferredCountries: null, PreferredFields: null);

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

    private static async Task<(Guid id, ApplicationUser user, UserProfile profile)> SeedAsync(
        ApplicationDbContext db,
        Action<ApplicationUser, UserProfile>? configure = null)
    {
        var id = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = id,
            Email = "u@test.local",
            NormalizedEmail = "U@TEST.LOCAL",
            UserName = "u@test.local",
            NormalizedUserName = "U@TEST.LOCAL",
            FirstName = "Test",
            LastName = "User",
            AccountStatus = AccountStatus.Active,
            PasswordHash = "PASSWORD-HASH",
        };
        var profile = new UserProfile { UserId = id };
        configure?.Invoke(user, profile);
        user.Profile = profile;
        db.Users.Add(user);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        return (id, user, profile);
    }

    // ── CR-PROF-07: Company verification-sensitive fields ───────────────────

    [Fact]
    public async Task Updating_a_verified_companys_legal_name_resets_verification_status()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db, (_, p) =>
        {
            p.OrganizationLegalName = "Acme Inc";
            p.OrganizationWebsite = "https://acme.example";
            p.OrganizationVerificationStatus = "Verified";
            p.OrganizationVerifiedAt = DateTimeOffset.UtcNow;
        });

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var fields = Empty() with { OrganizationLegalName = "Acme Holdings Ltd" };

        var dto = await sut.Handle(new UpdateProfileCommand(fields), default);

        dto.OrganizationVerificationStatus.Should().Be("PendingReview");
        dto.OrganizationLegalName.Should().Be("Acme Holdings Ltd");
        var fresh = await db.UserProfiles.FirstAsync(p => p.UserId == id);
        fresh.OrganizationVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Updating_a_verified_companys_website_resets_verification_status()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db, (_, p) =>
        {
            p.OrganizationLegalName = "Acme Inc";
            p.OrganizationWebsite = "https://acme.example";
            p.OrganizationVerificationStatus = "Verified";
        });

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var fields = Empty() with { OrganizationWebsite = "https://acme-new.example" };

        var dto = await sut.Handle(new UpdateProfileCommand(fields), default);

        dto.OrganizationVerificationStatus.Should().Be("PendingReview");
    }

    [Fact]
    public async Task Non_legal_field_updates_do_not_reset_verification()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db, (_, p) =>
        {
            p.OrganizationLegalName = "Acme Inc";
            p.OrganizationWebsite = "https://acme.example";
            p.OrganizationVerificationStatus = "Verified";
        });

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var fields = Empty() with { Biography = "Just a tagline" };

        var dto = await sut.Handle(new UpdateProfileCommand(fields), default);

        dto.OrganizationVerificationStatus.Should().Be("Verified");
    }

    // ── CR-PROF-11: Profile update cannot change role / status fields ───────

    [Fact]
    public async Task Profile_update_cannot_change_account_status()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db);

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var fields = Empty() with { FirstName = "Renamed" };
        await sut.Handle(new UpdateProfileCommand(fields), default);

        // The handler must leave AccountStatus untouched: there is no path
        // through UpdateProfileRequestDto that can carry a status change.
        var fresh = await db.Users.FirstAsync(u => u.Id == id);
        fresh.AccountStatus.Should().Be(AccountStatus.Active);
        fresh.ActiveRole.Should().BeNull();
    }

    // ── CR-PROF-08: Consultant professional fields ──────────────────────────

    [Fact]
    public async Task Consultant_professional_fields_are_persisted()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db);

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var fields = Empty() with
        {
            ProfessionalTitle = "Senior PhD Coach",
            YearsOfExperience = 12,
            ExpertiseTags = new[] { "PhD applications", "Statement of purpose" },
            Languages = new[] { "English", "Arabic" },
            Timezone = "Europe/London",
        };

        var dto = await sut.Handle(new UpdateProfileCommand(fields), default);

        dto.ProfessionalTitle.Should().Be("Senior PhD Coach");
        dto.YearsOfExperience.Should().Be(12);
        dto.ExpertiseTags.Should().BeEquivalentTo(["PhD applications", "Statement of purpose"]);
        dto.Languages.Should().BeEquivalentTo(["English", "Arabic"]);
        dto.Timezone.Should().Be("Europe/London");

        var fresh = await db.UserProfiles.FirstAsync(p => p.UserId == id);
        fresh.ProfessionalTitle.Should().Be("Senior PhD Coach");
        fresh.YearsOfExperience.Should().Be(12);
        fresh.ExpertiseTagsJson.Should().NotBeNull();
        fresh.LanguagesJson.Should().NotBeNull();
        fresh.Timezone.Should().Be("Europe/London");
    }

    // ── CR-PROF-06: hasPasswordCredential flag surfaces on the DTO ──────────

    [Fact]
    public async Task Has_password_credential_is_true_for_password_user()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db);

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var dto = await sut.Handle(new UpdateProfileCommand(Empty()), default);

        dto.HasPasswordCredential.Should().BeTrue();
    }

    [Fact]
    public async Task Has_password_credential_is_false_for_sso_only_user()
    {
        using var db = CreateDb();
        var (id, _, _) = await SeedAsync(db, (u, _) => u.PasswordHash = null);

        var sut = new UpdateProfileCommandHandler(db, User(id));
        var dto = await sut.Handle(new UpdateProfileCommand(Empty()), default);

        dto.HasPasswordCredential.Should().BeFalse();
    }
}
