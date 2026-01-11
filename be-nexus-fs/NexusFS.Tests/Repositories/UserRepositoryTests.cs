using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace NexusFS.Tests.Repositories;

public class UserRepositoryTests
{
    private static UserEntity CreateBasicUser(string username = "alice", string email = "alice@test.com") =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            Provider = "Basic",
            PasswordHash = "plaintext",
            Role = "User"
        };

    [Fact]
    public async Task AddAsync_HashesPassword_AndSetsDefaults()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "UserRepo_Add")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new UserRepository(context, new PasswordHasher<UserEntity>());

        var created = await repo.AddAsync(CreateBasicUser());

        created.Id.Should().NotBeNullOrEmpty();
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        created.IsActive.Should().BeTrue();
        created.PasswordHash.Should().NotBe("plaintext");
        created.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddAsync_Throws_WhenUsernameExists()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "UserRepo_DuplicateUsername")
            .Options;

        using var context = new NexusFSDbContext(options);
        context.Users.Add(CreateBasicUser());
        await context.SaveChangesAsync();

        var repo = new UserRepository(context, new PasswordHasher<UserEntity>());

        var act = async () => await repo.AddAsync(CreateBasicUser());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndHashesNewPassword()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "UserRepo_Update")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new UserRepository(context, new PasswordHasher<UserEntity>());
        var original = await repo.AddAsync(CreateBasicUser());
        var newPassword = "new-pass";

        var updateModel = new UserEntity
        {
            Id = original.Id,
            Username = "alice-updated",
            Email = "alice+updated@test.com",
            Provider = original.Provider,
            Role = "Admin",
            PasswordHash = newPassword,
            IsActive = false
        };

        await repo.UpdateAsync(updateModel);

        var stored = await context.Users.SingleAsync(u => u.Id == original.Id);
        stored.Username.Should().Be("alice-updated");
        stored.Email.Should().Be("alice+updated@test.com");
        stored.Role.Should().Be("Admin");
        stored.IsActive.Should().BeFalse();
        stored.UpdatedAt.Should().NotBeNull();

        var hasher = new PasswordHasher<UserEntity>();
        hasher.VerifyHashedPassword(stored, stored.PasswordHash!, newPassword)
            .Should().Be(PasswordVerificationResult.Success);
        hasher.VerifyHashedPassword(stored, stored.PasswordHash!, "plaintext")
            .Should().NotBe(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesUser()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "UserRepo_Delete")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new UserRepository(context, new PasswordHasher<UserEntity>());
        var user = await repo.AddAsync(CreateBasicUser());

        await repo.DeleteAsync(user.Id);

        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == user.Id);
        stored.DeletedAt.Should().NotBeNull();
        stored.IsActive.Should().BeFalse();
        (await repo.GetByIdAsync(user.Id)).Should().BeNull(); // filtered by DeletedAt
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsUser_WhenPasswordMatches()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "UserRepo_Validate")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new UserRepository(context, new PasswordHasher<UserEntity>());
        var created = await repo.AddAsync(CreateBasicUser(username: "bob", email: "bob@test.com"));

        var result = await repo.ValidateCredentialsAsync("bob", "plaintext");

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }
}
