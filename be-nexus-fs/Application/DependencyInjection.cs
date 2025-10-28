using Application.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Application Services
        services.AddSingleton<ErrorHandler>();
        services.AddSingleton<ConfigManager>();

        return services;
    }
}