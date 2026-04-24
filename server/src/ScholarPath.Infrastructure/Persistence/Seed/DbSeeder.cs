using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static class DbSeeder
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

        // 2) Demo users (dev only)
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
