using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Strongly-typed handle on the seeded users, so the downstream section
    /// seeders can reference real principals by role without re-querying.
    /// The four original demo accounts (admin/student/company/consultant
    /// @scholarpath.local) are the *primary* members; the extra accounts give
    /// every list view enough rows to look realistic and exercise the
    /// account-status filters.
    /// </summary>
    public sealed record DemoUsers
    {
        public required ApplicationUser PrimaryAdmin { get; init; }
        public required ApplicationUser SecondaryAdmin { get; init; }
        public required IReadOnlyList<ApplicationUser> Students { get; init; }
        public required IReadOnlyList<ApplicationUser> Companies { get; init; }
        public required IReadOnlyList<ApplicationUser> Consultants { get; init; }

        /// <summary>The demo <c>student@scholarpath.local</c> account.</summary>
        public ApplicationUser PrimaryStudent => Students[0];

        /// <summary>The demo <c>company@scholarpath.local</c> account.</summary>
        public ApplicationUser PrimaryScholarshipProvider => Companies[0];

        /// <summary>The demo <c>consultant@scholarpath.local</c> account.</summary>
        public ApplicationUser PrimaryConsultant => Consultants[0];
    }

    /// <summary>
    /// Seeds the extended demo user roster (extra students, companies,
    /// consultants and a second admin) plus a fully populated
    /// <see cref="UserProfile"/> for every demo user — the four originals
    /// included, since the original seeder created them without profiles.
    /// Idempotent: a user is created only if its email is free, and a profile
    /// is added only if the user has none.
    /// </summary>
    private static async Task<DemoUsers> SeedDemoUsersAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken ct)
    {
        // --- extra admin -------------------------------------------------
        var secondaryAdmin = await EnsureUserAsync(
            userManager, "ops.admin@scholarpath.local", "Admin123!",
            "Mariam", "Adel", "Admin", AccountStatus.Active, "EG", logger);

        // --- students (mix of account statuses) --------------------------
        var students = new List<ApplicationUser>
        {
            await EnsureUserAsync(userManager, "student@scholarpath.local", "Student123!", "Alaa", "Mostafa", "Student", AccountStatus.Active, "EG", logger),
            await EnsureUserAsync(userManager, "omar.student@scholarpath.local", "Student123!", "Omar", "Khalil", "Student", AccountStatus.Active, "JO", logger),
            await EnsureUserAsync(userManager, "lina.student@scholarpath.local", "Student123!", "Lina", "Haddad", "Student", AccountStatus.Active, "AE", logger),
            await EnsureUserAsync(userManager, "youssef.student@scholarpath.local", "Student123!", "Youssef", "Nabil", "Student", AccountStatus.Active, "EG", logger),
            await EnsureUserAsync(userManager, "sara.student@scholarpath.local", "Student123!", "Sara", "Tarek", "Student", AccountStatus.PendingApproval, "SA", logger),
            await EnsureUserAsync(userManager, "khaled.student@scholarpath.local", "Student123!", "Khaled", "Mansour", "Student", AccountStatus.Suspended, "MA", logger),
        };

        // --- companies (one suspended for the moderation demo) -----------
        var companies = new List<ApplicationUser>
        {
            await EnsureUserAsync(userManager, "company@scholarpath.local", "ScholarshipProvider123!", "Global Scholars", "Org", "ScholarshipProvider", AccountStatus.Active, "GB", logger),
            await EnsureUserAsync(userManager, "futurefund@scholarpath.local", "ScholarshipProvider123!", "FutureFund", "Foundation", "ScholarshipProvider", AccountStatus.Active, "US", logger),
            await EnsureUserAsync(userManager, "nilebridge@scholarpath.local", "ScholarshipProvider123!", "Nile Bridge", "Education", "ScholarshipProvider", AccountStatus.Active, "EG", logger),
            await EnsureUserAsync(userManager, "pendingco@scholarpath.local", "ScholarshipProvider123!", "Horizon Grants", "Pending", "ScholarshipProvider", AccountStatus.PendingApproval, "DE", logger),
        };

        // --- consultants (the marketplace) -------------------------------
        var consultants = new List<ApplicationUser>
        {
            await EnsureUserAsync(userManager, "consultant@scholarpath.local", "Consult123!", "Hana", "Farouk", "Consultant", AccountStatus.Active, "EG", logger),
            await EnsureUserAsync(userManager, "tarek.consultant@scholarpath.local", "Consult123!", "Tarek", "Saleh", "Consultant", AccountStatus.Active, "AE", logger),
            await EnsureUserAsync(userManager, "nour.consultant@scholarpath.local", "Consult123!", "Nour", "Abdullah", "Consultant", AccountStatus.Active, "JO", logger),
            await EnsureUserAsync(userManager, "james.consultant@scholarpath.local", "Consult123!", "James", "Carter", "Consultant", AccountStatus.Active, "CA", logger),
            await EnsureUserAsync(userManager, "deact.consultant@scholarpath.local", "Consult123!", "Rania", "Fouad", "Consultant", AccountStatus.Deactivated, "EG", logger),
        };

        var users = new DemoUsers
        {
            PrimaryAdmin = (await userManager.FindByEmailAsync("admin@scholarpath.local").ConfigureAwait(false))!,
            SecondaryAdmin = secondaryAdmin,
            Students = students,
            Companies = companies,
            Consultants = consultants,
        };

        await SeedUserProfilesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedTeamAccountsAsync(db, userManager, logger, ct).ConfigureAwait(false);
        await SeedGeneratedUsersAsync(db, userManager, logger, ct).ConfigureAwait(false);
        await SeedConsultantBiographyArAsync(db, logger, ct).ConfigureAwait(false);
        return users;
    }

    /// <summary>
    /// Seeds the project team's own demo accounts — one account per role for
    /// every team member, so each can sign in and exercise every role
    /// (Student, ScholarshipProvider, Consultant, Admin). Emails follow
    /// <c>{role}.{name}@scholarpath.local</c>; the password is the standard
    /// demo password for that role. Idempotent — re-running creates nothing
    /// that already exists.
    /// </summary>
    private static async Task SeedTeamAccountsAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken ct)
    {
        // (display name, email slug)
        var team = new[]
        {
            ("Tasneem", "tasneem"),
            ("Mimi", "mimi"),
            ("Nour", "nour"),
            ("Yousra", "yousra"),
            ("Nadia", "nadia"),
        };

        // (role, email slug, demo password)
        var roles = new[]
        {
            ("Student", "student", "Student123!"),
            ("ScholarshipProvider", "company", "ScholarshipProvider123!"),
            ("Consultant", "consultant", "Consult123!"),
            ("Admin", "admin", "Admin123!"),
        };

        var withProfile = (await db.UserProfiles
                .Select(p => p.UserId)
                .ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet();

        var profilesAdded = 0;
        foreach (var (displayName, nameSlug) in team)
        {
            foreach (var (role, roleSlug, password) in roles)
            {
                var email = $"{roleSlug}.{nameSlug}@scholarpath.local";
                var user = await EnsureUserAsync(
                    userManager, email, password, displayName, "Team",
                    role, AccountStatus.Active, "EG", logger).ConfigureAwait(false);

                if (!withProfile.Contains(user.Id))
                {
                    db.UserProfiles.Add(BuildTeamProfile(user, role));
                    withProfile.Add(user.Id);
                    profilesAdded++;
                }
            }
        }

        if (profilesAdded > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Seeded team accounts: {Members} members x {Roles} roles ({Profiles} profiles added)",
            team.Length, roles.Length, profilesAdded);
    }

    /// <summary>Builds a role-appropriate profile for a team demo account.</summary>
    private static UserProfile BuildTeamProfile(ApplicationUser user, string role)
    {
        var now = DateTimeOffset.UtcNow;
        var p = new UserProfile
        {
            UserId = user.Id,
            Timezone = "Africa/Cairo",
            Nationality = "EG",
            CreatedAt = now.AddDays(-30),
            ProfileCompletenessPercent = 85,
            Biography = $"ScholarPath project team member — {role} demo account.",
            BiographyAr = $"عضو فريق مشروع ScholarPath — حساب تجريبي بدور {role}.",
        };

        switch (role)
        {
            case "Student":
                p.AcademicLevel = AcademicLevel.Masters;
                p.FieldOfStudy = "Computer Science";
                p.CurrentInstitution = "Ain Shams University";
                p.Gpa = 3.7m;
                p.GpaScale = "4.0";
                p.PreferredCountriesJson = """["US","GB","DE","CA"]""";
                p.PreferredFieldsJson = """["Computer Science","Data Science"]""";
                break;

            case "ScholarshipProvider":
                p.OrganizationLegalName = $"{user.FirstName} Education Org";
                p.OrganizationVerificationStatus = "Verified";
                p.OrganizationVerifiedAt = now.AddDays(-20);
                break;

            case "Consultant":
                p.SessionFeeUsd = 50m;
                p.SessionDurationMinutes = 45;
                p.ExpertiseTagsJson = """["University Selection","Statement of Purpose","Scholarship Strategy"]""";
                p.LanguagesJson = """["en","ar"]""";
                p.ConsultantVerifiedAt = now.AddDays(-20);
                break;

            // Admin — the base profile (bio only) is enough.
            default:
                p.ProfileCompletenessPercent = 60;
                break;
        }

        return p;
    }

    /// <summary>
    /// Overload of the original <c>EnsureUserAsync</c> that also accepts an
    /// <see cref="AccountStatus"/> / country and RETURNS the user (created or
    /// pre-existing) so callers can build relationships against it.
    /// </summary>
    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string password, string firstName, string lastName, string role,
        AccountStatus status, string countryCode, ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            AccountStatus = status,
            IsOnboardingComplete = status is AccountStatus.Active or AccountStatus.Suspended or AccountStatus.Deactivated,
            ActiveRole = role,
            CountryOfResidence = countryCode,
            PreferredLanguage = "en",
            LastLoginAt = status == AccountStatus.Active ? DateTimeOffset.UtcNow.AddDays(-2) : null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-90),
        };

        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed user {Email}: {Errors}", email, string.Join(";", result.Errors.Select(e => e.Description)));
            // Re-fetch — a concurrent run may have created it.
            return (await userManager.FindByEmailAsync(email).ConfigureAwait(false))!;
        }

        await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        logger.LogInformation("Seeded user {Email} as {Role}", email, role);
        return user;
    }

    /// <summary>
    /// Creates a rich <see cref="UserProfile"/> for every demo user that lacks
    /// one. Critically this fills the CONSULTANT fields (<c>SessionFeeUsd</c>,
    /// <c>SessionDurationMinutes</c>, <c>ExpertiseTagsJson</c>,
    /// <c>LanguagesJson</c>, <c>Biography</c>) — without those the consultant
    /// marketplace renders blank cards.
    /// </summary>
    private static async Task SeedUserProfilesAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        var allUsers = new List<ApplicationUser> { users.PrimaryAdmin, users.SecondaryAdmin };
        allUsers.AddRange(users.Students);
        allUsers.AddRange(users.Companies);
        allUsers.AddRange(users.Consultants);

        var withProfileIds = await db.UserProfiles
            .Select(p => p.UserId)
            .ToListAsync(ct).ConfigureAwait(false);
        var withProfile = withProfileIds.ToHashSet();

        var added = 0;
        foreach (var u in allUsers)
        {
            if (withProfile.Contains(u.Id))
            {
                continue;
            }

            db.UserProfiles.Add(BuildProfile(u, users));
            added++;
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} user profiles", added);
    }

    private static UserProfile BuildProfile(ApplicationUser u, DemoUsers users)
    {
        var now = DateTimeOffset.UtcNow;
        var p = new UserProfile
        {
            UserId = u.Id,
            Timezone = "Africa/Cairo",
            Nationality = u.CountryOfResidence,
            CreatedAt = now.AddDays(-80),
            ProfileCompletenessPercent = 90,
        };

        // Admin profiles — minimal, just a bio.
        if (users.PrimaryAdmin.Id == u.Id || users.SecondaryAdmin.Id == u.Id)
        {
            p.Biography = "Platform administrator on the ScholarPath operations team.";
            p.ProfileCompletenessPercent = 60;
            return p;
        }

        // Student profiles — academic detail.
        var studentIndex = IndexOf(users.Students, u.Id);
        if (studentIndex >= 0)
        {
            var levels = new[] { AcademicLevel.Undergrad, AcademicLevel.Masters, AcademicLevel.Undergrad, AcademicLevel.HighSchool, AcademicLevel.Masters, AcademicLevel.PhD };
            var fields = new[] { "Computer Science", "Mechanical Engineering", "Public Health", "Economics", "Architecture", "Biotechnology" };
            var institutions = new[] { "Cairo University", "University of Jordan", "Khalifa University", "Ain Shams University", "King Saud University", "Mohammed V University" };
            var gpas = new[] { 3.8m, 3.5m, 3.9m, 3.2m, 3.7m, 3.95m };
            var i = studentIndex % levels.Length;

            p.Biography = $"Motivated {fields[i]} student looking for international scholarship opportunities and mentorship.";
            p.AcademicLevel = levels[i];
            p.FieldOfStudy = fields[i];
            p.CurrentInstitution = institutions[i];
            p.Gpa = gpas[i];
            p.GpaScale = "4.0";
            p.DateOfBirth = new DateOnly(2002, 4, 12).AddDays(i * 137);
            p.LinkedInUrl = $"https://www.linkedin.com/in/{Slugify(u.FirstName)}-{Slugify(u.LastName)}";
            p.PreferredCountriesJson = """["US","GB","DE","CA"]""";
            p.PreferredFieldsJson = $"""["{fields[i]}","Data Science"]""";
            return p;
        }

        // ScholarshipProvider profiles — organisation detail.
        var companyIndex = IndexOf(users.Companies, u.Id);
        if (companyIndex >= 0)
        {
            var legalNames = new[] { "Global Scholars Education Ltd.", "FutureFund Foundation Inc.", "Nile Bridge for Education", "Horizon Grants GmbH" };
            var verified = companyIndex < 3;
            p.Biography = "An organisation funding scholarships and reviewing student applications on ScholarPath.";
            p.OrganizationLegalName = legalNames[companyIndex % legalNames.Length];
            p.OrganizationRegistrationNumber = $"REG-{2018 + companyIndex}-{1000 + companyIndex * 7}";
            p.OrganizationWebsite = $"https://www.{Slugify(u.FirstName.Replace(" ", "", StringComparison.Ordinal))}.org";
            p.WebsiteUrl = p.OrganizationWebsite;
            p.OrganizationVerificationStatus = verified ? "Verified" : "Pending";
            p.OrganizationVerifiedAt = verified ? now.AddDays(-60) : null;
            // Companies are payees for review fees — give the verified ones Stripe Connect.
            p.StripeConnectAccountId = verified ? $"acct_demo_company_{companyIndex}" : null;
            p.StripeConnectStatus = verified ? StripeConnectStatus.Verified : StripeConnectStatus.None;
            p.StripeConnectOnboardedAt = verified ? now.AddDays(-58) : null;
            return p;
        }

        // Consultant profiles — THE marketplace fields. Must be populated.
        var consultantIndex = IndexOf(users.Consultants, u.Id);
        if (consultantIndex >= 0)
        {
            var bios = CuratedConsultantBios;
            var expertise = new[]
            {
                """["Statement of Purpose","Interview Prep","University Selection"]""",
                """["Fully Funded Scholarships","PhD Applications","UK Admissions"]""",
                """["CV Review","STEM Applications","Funding Strategy"]""",
                """["Research Proposals","PostDoc Applications","Fellowships"]""",
                """["General Guidance"]""",
            };
            var langs = new[]
            {
                """["en","ar"]""",
                """["en","ar","fr"]""",
                """["en","ar"]""",
                """["en"]""",
                """["en","ar"]""",
            };
            var fees = new[] { 45m, 60m, 35m, 80m, 50m };
            var durations = new[] { 45, 60, 30, 60, 45 };
            var i = consultantIndex % bios.Length;
            var verified = consultantIndex < 4; // the deactivated one is not verified

            p.Biography = bios[i].En;
            p.BiographyAr = bios[i].Ar;
            p.SessionFeeUsd = fees[i];
            p.SessionDurationMinutes = durations[i];
            p.ExpertiseTagsJson = expertise[i];
            p.LanguagesJson = langs[i];
            p.ConsultantVerifiedAt = verified ? now.AddDays(-70) : null;
            p.LinkedInUrl = $"https://www.linkedin.com/in/{Slugify(u.FirstName)}-consultant";
            p.WebsiteUrl = $"https://www.{Slugify(u.FirstName)}-advising.com";
            // Consultants are payees for booking fees — give the verified ones Stripe Connect.
            p.StripeConnectAccountId = verified ? $"acct_demo_consultant_{consultantIndex}" : null;
            p.StripeConnectStatus = verified ? StripeConnectStatus.Verified : StripeConnectStatus.Pending;
            p.StripeConnectOnboardedAt = verified ? now.AddDays(-68) : null;
            return p;
        }

        p.Biography = "ScholarPath user.";
        return p;
    }

    private static int IndexOf(IReadOnlyList<ApplicationUser> list, Guid id)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Lower-cases a name fragment for use inside a demo URL. The lowering is a
    /// URL-format requirement, not a culture-sensitive display concern — hence
    /// the deliberate CA1308 suppression, matching the pattern used in
    /// <c>FileStorageService</c>.
    /// </summary>
    private static string Slugify(string value)
    {
#pragma warning disable CA1308 // URL slug — invariant lower-casing is intentional.
        return value.ToLowerInvariant();
#pragma warning restore CA1308
    }

    /// <summary>Bilingual bios for the five hand-curated demo consultants.</summary>
    private static readonly (string En, string Ar)[] CuratedConsultantBios =
    [
        ("Former admissions officer with 8 years' experience guiding students into top-50 universities. I help with statements of purpose, school selection and interview prep.",
         "مسؤولة قبول سابقة بخبرة 8 سنوات في إرشاد الطلاب إلى أفضل 50 جامعة. أساعد في خطابات الغرض واختيار الجامعة والتحضير للمقابلات."),
        ("Scholarship strategist specialising in fully funded Master's and PhD programmes in the UK and Gulf. 200+ students mentored.",
         "خبير استراتيجيات منح متخصص في برامج الماجستير والدكتوراه المموّلة بالكامل في بريطانيا والخليج. أرشدتُ أكثر من 200 طالب."),
        ("Career and study-abroad coach focused on STEM applicants. I review CVs, recommendation strategy and funding applications.",
         "مدرّب مهني وللدراسة بالخارج متخصص في طلاب التخصصات العلمية. أراجع السير الذاتية واستراتيجية التوصيات وطلبات التمويل."),
        ("PhD holder and peer reviewer. I support research-proposal writing and PostDoc / fellowship applications for North America.",
         "حاصل على الدكتوراه ومراجع أقران. أدعم كتابة المقترحات البحثية وطلبات ما بعد الدكتوراه والزمالات لأمريكا الشمالية."),
        ("Education consultant on a career break — currently not accepting new bookings.",
         "مستشارة تعليمية في إجازة مهنية — لا تقبل حجوزات جديدة حالياً."),
    ];

    /// <summary>
    /// Back-fills <see cref="UserProfile.BiographyAr"/> for consultant profiles
    /// seeded before the bilingual-bio change. Matches the stored English bio
    /// against the seed pools. Idempotent — only rows still missing the Arabic
    /// bio are touched.
    /// </summary>
    private static async Task SeedConsultantBiographyArAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (en, ar) in GenConsultantBios)
        {
            map[en] = ar;
        }
        foreach (var (en, ar) in CuratedConsultantBios)
        {
            map[en] = ar;
        }
        foreach (var role in new[] { "Student", "ScholarshipProvider", "Consultant", "Admin" })
        {
            map[$"ScholarPath project team member — {role} demo account."]
                = $"عضو فريق مشروع ScholarPath — حساب تجريبي بدور {role}.";
        }

        var profiles = await db.UserProfiles
            .Where(p => p.BiographyAr == null && p.SessionFeeUsd != null && p.Biography != null)
            .ToListAsync(ct).ConfigureAwait(false);

        var updated = 0;
        foreach (var p in profiles)
        {
            if (p.Biography is not null && map.TryGetValue(p.Biography, out var ar))
            {
                p.BiographyAr = ar;
                updated++;
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation("Back-filled {N} Arabic consultant bios", updated);
    }
}
