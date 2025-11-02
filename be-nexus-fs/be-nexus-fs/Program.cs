using Application;
using Infrastructure;
using Infrastructure.Configuration;
using Scalar.AspNetCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Clerk options - prioritize environment variables
var clerkOptions = new ClerkOptions();
builder.Configuration.GetSection(ClerkOptions.SectionName).Bind(clerkOptions);

// Override with environment variables if they exist
clerkOptions.ApiKey = Environment.GetEnvironmentVariable("CLERK_API_KEY") ?? clerkOptions.ApiKey;
clerkOptions.JwksUrl = Environment.GetEnvironmentVariable("CLERK_JWKS_URL") ?? clerkOptions.JwksUrl;
clerkOptions.Issuer = Environment.GetEnvironmentVariable("CLERK_ISSUER") ?? clerkOptions.Issuer;
clerkOptions.Audience = Environment.GetEnvironmentVariable("CLERK_AUDIENCE") ?? clerkOptions.Audience;

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = clerkOptions.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(clerkOptions.Audience),
            ValidAudience = clerkOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        
        // Handle authentication events
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // This event is called after successful token validation
                Console.WriteLine("Token validated successfully!");
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Auth failed: {context.Exception?.Message}");
                context.Response.Headers["Token-Expired"] = "true";
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                Console.WriteLine("Token received for validation");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register Clerk configuration
builder.Services.Configure<ClerkOptions>(options =>
{
    options.ApiKey = clerkOptions.ApiKey;
    options.JwksUrl = clerkOptions.JwksUrl;
    options.Issuer = clerkOptions.Issuer;
    options.Audience = clerkOptions.Audience;
    options.BaseUrl = clerkOptions.BaseUrl;
    options.ApiVersion = clerkOptions.ApiVersion;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Frontend:Url"] ?? "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


var app = builder.Build();

// Configure JWKS for Clerk authentication
var jwtBearerOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
var options = jwtBearerOptions.Get(JwtBearerDefaults.AuthenticationScheme);

// Manually set up the JWKS configuration for Clerk
var httpClient = new HttpClient();
var jwksJson = await httpClient.GetStringAsync(clerkOptions.JwksUrl);
var jwks = new JsonWebKeySet(jwksJson);

options.TokenValidationParameters.IssuerSigningKeys = jwks.Keys;

app.MapOpenApi();
    
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("NexusFS API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});


app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
