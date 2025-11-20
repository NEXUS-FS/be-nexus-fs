using Application.DTOs.User;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Services.Observability;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class DatabaseSeeder
    {
        private readonly NexusFSDbContext _context;
        private readonly Logger _logger;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<UserEntity> _passwordHasher;

        public DatabaseSeeder(
             NexusFSDbContext context,
             Logger logger,
             IConfiguration config,
             IPasswordHasher<UserEntity> passwordHasher)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _passwordHasher = passwordHasher;
        }


        public async Task SeedAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("DatabaseSeeder: Users already exist, skipping seeding.");
                return;
            }

            _logger.LogInformation("DatabaseSeeder: No users found, creating default admin.");

            var adminUsername = _config["Seed:Admin:Username"] ?? "Admin";
            var adminPassword = _config["Seed:Admin:Password"] ?? "Admin@123";

            //task did not require but an user as admin has also...  email .. so..

            var adminEmail = _config["Seed:Admin:Email"] ?? "admin@example.com";


            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid().ToString(), //this will assure unique.
                Username = adminUsername,
                Email = adminEmail,
                Role = "Admin",
                Provider = "Basic", //or smth else
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };


            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, adminPassword);

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"DatabaseSeeder: Default admin '{adminUsername}' created successfully.");

        }


    }

}

