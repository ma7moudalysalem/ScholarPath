using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        // Migrations are applied once by CustomWebApplicationFactory.InitializeAsync.
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    protected async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    protected static IApplicationDbContext GetDb(IServiceProvider sp) =>
        sp.GetRequiredService<IApplicationDbContext>();

    protected static TestCurrentUserService GetCurrentUser(IServiceProvider sp) =>
        sp.GetRequiredService<TestCurrentUserService>();

    /// <summary>
    /// Makes a seeded consultant a genuinely eligible consultant: ensures the
    /// <c>Consultant</c> Identity role exists, adds the user to it, and stamps
    /// <see cref="UserProfile.ConsultantVerifiedAt"/>. Required now that
    /// <c>RequestBooking</c> (and the marketplace / availability paths) reject
    /// consultants who are not verified/approved. Idempotent.
    /// </summary>
    protected static async Task EnsureEligibleConsultantAsync(IServiceProvider sp, Guid consultantId)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "CONSULTANT");
        if (role is null)
        {
            role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = "Consultant",
                NormalizedName = "CONSULTANT",
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var alreadyInRole = await db.UserRoles
            .AnyAsync(ur => ur.UserId == consultantId && ur.RoleId == role.Id);
        if (!alreadyInRole)
        {
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = consultantId, RoleId = role.Id });
        }

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == consultantId);
        if (profile is not null)
        {
            profile.ConsultantVerifiedAt ??= DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
