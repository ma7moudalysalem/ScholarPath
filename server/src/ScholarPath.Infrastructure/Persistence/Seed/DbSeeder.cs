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
        ["SuperAdmin", "Admin", "Student", "ScholarshipProvider", "Consultant", "Unassigned"];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        // Azure SQL Basic / Standard tiers can take 30+ seconds to handle the
        // larger bulk inserts the seeder does (users with profiles, 1000
        // scholarships, etc.). Raise the EF command timeout to 5 minutes for
        // the lifetime of this seeding context so we don't get spurious
        // "Execution Timeout Expired" SqlExceptions on first boot.
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

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
        await EnsureUserAsync(userManager, "company@scholarpath.local", "ScholarshipProvider123!", "Global Scholars", "Org", "ScholarshipProvider", logger);
        await EnsureUserAsync(userManager, "consultant@scholarpath.local", "Consult123!", "Hana", "Farouk", "Consultant", logger);

        // 2b) Bootstrap a SuperAdmin. Granting/revoking the Admin role is guarded to
        // the SuperAdmin tier (so an Admin can't mint another Admin — anti
        // self-escalation). Without a SuperAdmin existing, "add admin" is
        // permanently blocked with a 403. Promote the primary demo admin — the JWT
        // role claim is the user's ActiveRole, so it must be SuperAdmin for the
        // IsInRole("SuperAdmin") check to pass. Idempotent: safe on every startup.
        var superAdmin = await userManager.FindByEmailAsync("admin@scholarpath.local").ConfigureAwait(false);
        if (superAdmin is not null)
        {
            if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin").ConfigureAwait(false))
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin").ConfigureAwait(false);
            if (superAdmin.ActiveRole != "SuperAdmin")
            {
                superAdmin.ActiveRole = "SuperAdmin";
                await userManager.UpdateAsync(superAdmin).ConfigureAwait(false);
            }
            logger.LogInformation("Ensured SuperAdmin bootstrap for {Email}.", superAdmin.Email);
        }

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
                    PaymentType = PaymentType.ScholarshipProviderReview,
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

        // Rich bilingual consultant reviews + provider rating snapshots so the
        // marketplace and rating pages look realistic at scale (demo/screenshots).
        await SeedBulkReviewsAsync(db, users, logger, ct).ConfigureAwait(false);

        await SeedCommunityAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedChatAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedPaymentsAsync(db, users, applications, bookings, logger, ct).ConfigureAwait(false);

        var resources = await SeedResourcesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedResourceEngagementAsync(db, users, resources, logger, ct).ConfigureAwait(false);

        await SeedDocumentsAsync(db, users, applications, logger, ct).ConfigureAwait(false);
        await SeedNotificationsAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedSuccessStoriesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedAiAsync(db, users, scholarships, logger, ct).ConfigureAwait(false);

        // Deep "ran for a year" enrichment: bulk bilingual community threads +
        // replies + votes + moderation queue, a populated bilingual document
        // vault, a year of audit-log activity, and the admin low-rating queues.
        await SeedEnrichmentAsync(db, users, logger, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// DESTRUCTIVE one-shot: deletes every data row from every application table
    /// (keeping the schema and <c>__EFMigrationsHistory</c>) so the idempotent
    /// <see cref="SeedAsync"/> repopulates a fresh demo dataset on the same boot.
    /// Gated behind the <c>ResetAndReseedData</c> config flag in Program.cs —
    /// never call this unguarded. No-op on a non-relational provider (tests).
    /// </summary>
    public static async Task ResetDemoDataAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            return;
        }

        logger.LogWarning(
            "ResetAndReseedData=true — WIPING all application data before reseeding. " +
            "This is destructive and intended only for a demo-data refresh.");
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        // Dynamic SQL: disable every FK, delete every user-table row except the
        // migration history + DataProtection keys, then re-enable the FKs. Ordering
        // is irrelevant because constraints are off during the deletes.
        const string sql = @"
-- Required so DELETE works against tables that carry filtered indexes
-- (e.g. the active-booking unique index) regardless of the caller's session.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @excluded TABLE (name sysname);
INSERT INTO @excluded(name) VALUES ('__EFMigrationsHistory'), ('DataProtectionKeys');

DECLARE @sql NVARCHAR(MAX);

SET @sql = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + ' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name NOT IN (SELECT name FROM @excluded);
EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql += 'DELETE FROM ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + ';' + CHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name NOT IN (SELECT name FROM @excluded);
EXEC sp_executesql @sql;

SET @sql = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) + ' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name NOT IN (SELECT name FROM @excluded);
EXEC sp_executesql @sql;
";

        await db.Database.ExecuteSqlRawAsync(sql, ct).ConfigureAwait(false);
        logger.LogWarning("Data wipe complete — the seeder will now repopulate a fresh demo dataset.");
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
            // Master payments switch: when off, the platform runs in fully-free
            // mode — every fee on every flow is forced to 0, Stripe is never
            // called, billing dashboards show a "payments disabled" banner. The
            // per-feature allow-free toggles below are moot when this is off.
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "payments.enabled",
                Value = "true",
                ValueType = PlatformSettingType.Boolean,
                Category = "Payments",
                DescriptionEn = "Master toggle. When off, the platform runs fully free — all fees become 0, Stripe is bypassed everywhere.",
                DescriptionAr = "المفتاح الرئيسي. عند إيقافه تعمل المنصة مجاناً بالكامل — جميع الرسوم تصبح صفر، ولا يُستدعى Stripe.",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            // PB-005R: lets the admin disable free in-app scholarship listings
            // platform-wide. When off, a ScholarshipProvider creating or updating an in-app
            // scholarship must set a Review Service Fee > 0.
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "payments.allowFreeScholarships",
                Value = "true",
                ValueType = PlatformSettingType.Boolean,
                Category = "Payments",
                DescriptionEn = "Allow companies to mark in-app scholarships as free (review fee = 0).",
                DescriptionAr = "السماح للشركات بجعل المنح داخل المنصة مجانية (رسوم المراجعة = 0).",
                CreatedAt = DateTimeOffset.UtcNow,
            },
            // PB-006R: lets the admin disable free consultant sessions platform-
            // wide. When off, a Consultant editing their profile or signing up
            // must set a Session Fee > 0.
            new PlatformSetting
            {
                Id = Guid.NewGuid(),
                Key = "payments.allowFreeConsultantSessions",
                Value = "true",
                ValueType = PlatformSettingType.Boolean,
                Category = "Payments",
                DescriptionEn = "Allow consultants to offer free sessions (session fee = 0).",
                DescriptionAr = "السماح للمستشارين بتقديم جلسات مجانية (رسوم الجلسة = 0).",
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
