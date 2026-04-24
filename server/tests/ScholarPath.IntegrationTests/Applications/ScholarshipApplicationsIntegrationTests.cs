using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit; // مهم جداً للـ IAsyncLifetime

namespace ScholarPath.IntegrationTests.Applications;

public class ScholarshipApplicationsFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 1. تعريف الحاوية (Docker Container)
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // 2. تشغيل الحاوية قبل بدء الاختبارات (IAsyncLifetime)
    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // إزالة DbContext الحقيقي
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // إضافة DbContext الخاص بالتيست باستخدام الـ Connection String من Docker
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
    }

    // 3. الحل العبقري للتخلص من تضارب الـ Dispose:
    // نستخدم ميثود الـ xUnit الرسمية لتنظيف الموارد الخارجية (Docker)
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    // 4. ميثود الـ Factory الأساسية لتنظيف السيرفر (ValueTask)
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
