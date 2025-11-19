using Application.Utils;
using Application.UseCases.Users.Queries;
using Application.UseCases.Users.CommandsHandler;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.AuditLogs.Queries.GetAuditLogs;
using FluentValidation;
using FluentValidation.AspNetCore;
using Application.DTOs.FileOperations.Validators;
using Application.UseCases.FileOperations.CommandsHandler;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Application Services
        services.AddSingleton<ErrorHandler>();
        services.AddSingleton<ConfigManager>();
        
        // User Command Handlers
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();
        services.AddScoped<LoginUserHandler>();

        // User Query Handlers
        services.AddScoped<GetUserByIdHandler>();
        services.AddScoped<GetAllUsersHandler>();
        services.AddScoped<GetUserByUsernameHandler>();
        services.AddScoped<GetUserByEmailHandler>();

        // Audit Log Query Handlers
        services.AddScoped<GetAuditLogsHandler>();

        // File Operation Command Handlers
        services.AddScoped<ReadFileHandler>();
        services.AddScoped<WriteFileHandler>();
        services.AddScoped<DeleteFileHandler>();
        services.AddScoped<ListFilesHandler>();

        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<ReadFileRequestValidator>();

        return services;
    }
}