using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Comprehensive, idempotent demo-data seeder. Split across several partial-class
/// files (one per domain area) so the file stays navigable:
/// <list type="bullet">
///   <item><c>DbSeeder.cs</c> — orchestration, roles, categories, configs, settings.</item>
///   <item><c>DbSeeder.Users.cs</c> — demo users + profiles across every role/status.</item>
///   <item><c>DbSeeder.Scholarships.cs</c> — scholarships, children, saved/bookmarked.</item>
///   <item><c>DbSeeder.Applications.cs</c> — application trackers across every status.</item>
///   <item><c>DbSeeder.Consultants.cs</c> — availability, bookings, consultant reviews.</item>
///   <item><c>DbSeeder.Community.cs</c> — forum categories/posts/votes/flags.</item>
///   <item><c>DbSeeder.Chat.cs</c> — conversations, messages, user blocks.</item>
///   <item><c>DbSeeder.Payments.cs</c> — payments, payouts, company reviews.</item>
///   <item><c>DbSeeder.Resources.cs</c> — resources, chapters, bookmarks, progress.</item>
///   <item><c>DbSeeder.Misc.cs</c> — documents, notifications, success stories, AI, etc.</item>
/// </list>
/// Every section checks whether ITS OWN data already exists before inserting, so
/// re-running <see cref="SeedAsync"/> on every app startup is a safe no-op once
/// the database is seeded.
/// </summary>
public static partial class DbSeeder
{
    public static readonly string[] SeededRoles =
        ["Admin", "Student", "Company", "Consultant", "Unassigned"];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        // 1) Roles
        foreach (var roleName in SeededRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = $"{roleName} role",
                    CreatedAt = DateTimeOffset.UtcNow,
                }).ConfigureAwait(false);
                logger.LogInformation("Seeded role {Role}", roleName);
            }
        }

        // 2) Demo users (the original four — login credentials documented in the README)
        await EnsureUserAsync(userManager, "admin@scholarpath.local", "Admin123!", "Admin", "User", "Admin", logger);
        await EnsureUserAsync(userManager, "student@scholarpath.local", "Student123!", "Alaa", "Mostafa", "Student", logger);
        await EnsureUserAsync(userManager, "company@scholarpath.local", "Company123!", "Global Scholars", "Org", "Company", logger);
        await EnsureUserAsync(userManager, "consultant@scholarpath.local", "Consult123!", "Hana", "Farouk", "Consultant", logger);

        // 3) Scholarship categories (bilingual)
        if (!await db.Categories.AnyAsync(ct).ConfigureAwait(false))
        {
            db.Categories.AddRange(
                new Category { NameEn = "STEM", NameAr = "العلوم والتكنولوجيا", Slug = "stem", DisplayOrder = 1, IconKey = "atom", CreatedAt = DateTimeOffset.UtcNow },
                new Category { NameEn = "Arts & Humanities", NameAr = "الفنون والإنسانيات", Slug = "arts-humanities", DisplayOrder = 2, IconKey = "palette", CreatedAt = DateTimeOffset.UtcNow },
                new Category { NameEn = "Business", NameAr = "الأعمال", Slug = "business", DisplayOrder = 3, IconKey = "briefcase", CreatedAt = DateTimeOffset.UtcNow },
                new Category { NameEn = "Medical", NameAr = "الطب", Slug = "medical", DisplayOrder = 4, IconKey = "stethoscope", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded {N} categories", 4);
        }

        // 4) Default profit share configs (10% / 15%)
        if (!await db.ProfitShareConfigs.AnyAsync(ct).ConfigureAwait(false))
        {
            var adminUser = await userManager.FindByEmailAsync("admin@scholarpath.local").ConfigureAwait(false);
            var adminId = adminUser?.Id ?? Guid.Empty;
            db.ProfitShareConfigs.AddRange(
                new ProfitShareConfig
                {
                    PaymentType = PaymentType.ConsultantBooking,
                    Percentage = 0.10m,
                    EffectiveFrom = DateTimeOffset.UtcNow,
                    SetByAdminId = adminId,
                    Notes = "Default 10% for consultant bookings",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new ProfitShareConfig
                {
                    PaymentType = PaymentType.CompanyReview,
                    Percentage = 0.15m,
                    EffectiveFrom = DateTimeOffset.UtcNow,
                    SetByAdminId = adminId,
                    Notes = "Default 15% for company reviews",
                    CreatedAt = DateTimeOffset.UtcNow,
                });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded default profit share configs");
        }

        // 5) Default platform settings (PB-011) — idempotent per key, so new
        //    defaults can be appended over time without disturbing edited rows.
        await SeedPlatformSettingsAsync(db, logger, ct).ConfigureAwait(false);

        // 6) Comprehensive demo dataset — each section is independently idempotent
        //    and seeds in dependency order (users first, then everything that
        //    references them). Wrapped in try/catch nothing here — a failure in a
        //    later section must not silently skip earlier ones; each section's
        //    own AnyAsync guard makes a re-run safe.
        var users = await SeedDemoUsersAsync(db, userManager, logger, ct).ConfigureAwait(false);
        var categories = await db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(ct).ConfigureAwait(false);

        await SeedExpertiseTagsAsync(db, logger, ct).ConfigureAwait(false);
        await SeedUpgradeRequestsAsync(db, users, logger, ct).ConfigureAwait(false);

        var scholarships = await SeedScholarshipsAsync(db, users, categories, logger, ct).ConfigureAwait(false);
        await SeedSavedScholarshipsAsync(db, users, scholarships, logger, ct).ConfigureAwait(false);

        var applications = await SeedApplicationsAsync(db, users, scholarships, logger, ct).ConfigureAwait(false);

        var bookings = await SeedConsultantModuleAsync(db, users, logger, ct).ConfigureAwait(false);

        await SeedCommunityAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedChatAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedPaymentsAsync(db, users, applications, bookings, logger, ct).ConfigureAwait(false);

        var resources = await SeedResourcesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedResourceEngagementAsync(db, users, resources, logger, ct).ConfigureAwait(false);

        await SeedDocumentsAsync(db, users, applications, logger, ct).ConfigureAwait(false);
        await SeedNotificationsAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedSuccessStoriesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedAiAsync(db, users, scholarships, logger, ct).ConfigureAwait(false);
    }

    private static async Task SeedPlatformSettingsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var defaults = new[]
        {
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "maintenance.enabled",
                Value = "false",
                ValueType = PlatformSettingType.Boolean,
                Category = "Access",
                DescriptionEn = "Put the platform in maintenance mode.",
                DescriptionAr = "تفعيل وضع الصيانة للمنصة.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "registration.open",
                Value = "true",
                ValueType = PlatformSettingType.Boolean,
                Category = "Access",
                DescriptionEn = "Allow new users to register.",
                DescriptionAr = "السماح بتسجيل مستخدمين جدد.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "support.email",
                Value = "support@scholarpath.local",
                ValueType = PlatformSettingType.Text,
                Category = "Support",
                DescriptionEn = "Public support contact email.",
                DescriptionAr = "البريد الإلكتروني للدعم.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "platform.announcement",
                Value = string.Empty,
                ValueType = PlatformSettingType.Text,
                Category = "General",
                DescriptionEn = "Site-wide announcement banner text.",
                DescriptionAr = "نص شريط الإعلان العام.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "ai.features.enabled",
                Value = "true",
                ValueType = PlatformSettingType.Boolean,
                Category = "AI",
                DescriptionEn = "Globally enable AI features.",
                DescriptionAr = "تفعيل ميزات الذكاء الاصطناعي عالمياً.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
        };

        var existingKeys = await db.PlatformSettings
            .Select(s => s.Key)
            .ToListAsync(ct).ConfigureAwait(false);

        var toAdd = defaults.Where(s => !existingKeys.Contains(s.Key)).ToList();
        if (toAdd.Count == 0)
        {
            return;
        }

        db.PlatformSettings.AddRange(toAdd);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} default platform settings", toAdd.Count);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string password, string firstName, string lastName, string role, ILogger logger)
    {
        if (await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            AccountStatus = AccountStatus.Active,
            IsOnboardingComplete = true,
            ActiveRole = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed user {Email}: {Errors}", email, string.Join(";", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        logger.LogInformation("Seeded user {Email} as {Role}", email, role);
    }
}
