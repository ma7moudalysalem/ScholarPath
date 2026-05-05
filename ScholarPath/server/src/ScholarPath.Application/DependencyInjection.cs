using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Behaviors;
using Microsoft.Extensions.Configuration;
using ScholarPath.Application.Common.Models;
namespace ScholarPath.Application;

public static class DependencyInjection
{
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        services.AddAutoMapper(assembly);

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        return services;
    }
}
