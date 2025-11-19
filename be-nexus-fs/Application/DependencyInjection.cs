using Application.Utils;
using Application.UseCases.Users.Queries;
using Application.UseCases.Users.CommandsHandler;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.AuditLogs.Queries.GetAuditLogs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Application.DTOs.FileOperations.Validators;

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

        services.AddScoped<GetAuditLogsHandler>();

        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<ReadFileRequestValidator>();

        return services;
    }
}