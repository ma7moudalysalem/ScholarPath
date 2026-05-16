using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Interfaces;

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
}
