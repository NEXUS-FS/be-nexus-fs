using Application.Utils;
using Application.UseCases.Users.Queries;
using Application.UseCases.Users.CommandsHandler;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Application Services
        services.AddSingleton<ErrorHandler>();
        services.AddSingleton<ConfigManager>();
        
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();
        services.AddScoped<LoginUserHandler>();

        // Query Handlers
        services.AddScoped<GetUserByIdHandler>();
        services.AddScoped<GetAllUsersHandler>();
        services.AddScoped<GetUserByUsernameHandler>();
        services.AddScoped<GetUserByEmailHandler>();

        return services;
    }
}