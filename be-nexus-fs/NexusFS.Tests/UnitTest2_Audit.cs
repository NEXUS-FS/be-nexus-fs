using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;


namespace NexusFS.Tests
{
    public class AuditLoggingTests
    {
        private NexusFSDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<NexusFSDbContext>()
                .UseInMemoryDatabase(databaseName: "AuditLogs")
                .Options;

            return new NexusFSDbContext(options);
        }

        [Fact]
        public async Task AddingEntity_ShouldCreateAuditLog()
        {
            // we do this in memory..
            var context = GetInMemoryDbContext();
            var repository = new AuditLogRepository(context);

            var user = new UserEntity
            {
                Id = "user1",
                Username = "testuser",
                Email = "test@example.com",
                Role = "User",
                Provider = "Basic",
                IsActive = true,
                CreatedAt = System.DateTime.UtcNow
            };

            context.Users.Add(user);

            // act
            await context.SaveChangesAsync(); // this should trigger audit logging

            var logs = await repository.GetRecentAsync();

            foreach (var logi in logs)
            {
                //for debugging purposes.
                Console.WriteLine($"Action: {logi.Action} \n Entity: {logi.ResourcePath} \n Details: {logi.Details}\n \n");
            }
            // Assert
            Assert.NotEmpty(logs);
            var log = logs.First();

            Assert.Equal("Added", log.Action);
            Assert.Equal("UserEntity", log.ResourcePath);
            Assert.Equal("SYSTEM", log.UserId); // we need a way to set this and.. get this.. TODO.
            Assert.NotNull(log.Details);
            Assert.True(log.Timestamp <= System.DateTime.UtcNow);
        }
    }
}
