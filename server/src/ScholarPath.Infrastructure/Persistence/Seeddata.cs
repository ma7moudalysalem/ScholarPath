using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // ===== Seed: Roles =====
        await SeedRolesAsync(roleManager);

        // ===== Seed: Admin Account =====
        await SeedAdminAsync(userManager);

        // ===== Seed: Categories (System Tags) =====
        await SeedCategoriesAsync(context);

        await context.SaveChangesAsync();
    }

    // ------------------------------------------------------------------
    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = ["Admin", "Student", "Consultant", "Company"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    // ------------------------------------------------------------------
    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@scholarpath.com";

        // Skip if already exists
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "ScholarPath",
            LastName = "Admin",
            Role = UserRole.Admin,
            AccountStatus = AccountStatus.Active,
            IsOnboardingComplete = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456!");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    // ------------------------------------------------------------------
    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Name = "Undergraduate",    CreatedAt = DateTime.UtcNow },
            new() { Name = "Postgraduate",     CreatedAt = DateTime.UtcNow },
            new() { Name = "PhD",              CreatedAt = DateTime.UtcNow },
            new() { Name = "STEM",             CreatedAt = DateTime.UtcNow },
            new() { Name = "Humanities",       CreatedAt = DateTime.UtcNow },
            new() { Name = "Medicine",         CreatedAt = DateTime.UtcNow },
            new() { Name = "Engineering",      CreatedAt = DateTime.UtcNow },
            new() { Name = "Business",         CreatedAt = DateTime.UtcNow },
            new() { Name = "Arts & Design",    CreatedAt = DateTime.UtcNow },
            new() { Name = "Law",              CreatedAt = DateTime.UtcNow },
            new() { Name = "Fully Funded",     CreatedAt = DateTime.UtcNow },
            new() { Name = "Partial Funding",  CreatedAt = DateTime.UtcNow },
            new() { Name = "Egypt",            CreatedAt = DateTime.UtcNow },
            new() { Name = "Europe",           CreatedAt = DateTime.UtcNow },
            new() { Name = "USA",              CreatedAt = DateTime.UtcNow },
            new() { Name = "Asia",             CreatedAt = DateTime.UtcNow },
        };

        await context.Categories.AddRangeAsync(categories);
    }
}
