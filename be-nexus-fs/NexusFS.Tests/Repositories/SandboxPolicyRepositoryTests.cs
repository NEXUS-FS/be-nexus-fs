using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace NexusFS.Tests.Repositories;

public class SandboxPolicyRepositoryTests
{
    [Fact]
    public async Task GetPolicyForUserAsync_ReturnsNull_WhenNotFound()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "SandboxPolicy_None")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new SandboxPolicyRepository(context);

        var result = await repo.GetPolicyForUserAsync("missing-user");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetPolicyAsync_Inserts_WhenMissing()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "SandboxPolicy_Insert")
            .Options;

        using var context = new NexusFSDbContext(options);
        var repo = new SandboxPolicyRepository(context);
        var policy = new SandboxPolicy
        {
            UserId = "user-1",
            IsReadOnly = true,
            MaxPathLength = 42,
            AllowDotFiles = true,
            BlockedFileExtensions = new List<string> { ".exe", ".bat" }
        };

        await repo.SetPolicyAsync(policy);

        var stored = await context.SandboxPolicies.SingleAsync();
        stored.UserId.Should().Be("user-1");
        stored.IsReadOnly.Should().BeTrue();
        stored.MaxPathLength.Should().Be(42);
        stored.AllowDotFiles.Should().BeTrue();
        stored.BlockedFileExtensions.Should().BeEquivalentTo(new[] { ".exe", ".bat" });
    }

    [Fact]
    public async Task SetPolicyAsync_Updates_WhenExisting()
    {
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: "SandboxPolicy_Update")
            .Options;

        using var context = new NexusFSDbContext(options);
        var existing = new SandboxPolicy
        {
            UserId = "user-2",
            IsReadOnly = false,
            MaxPathLength = 10,
            AllowDotFiles = false,
            BlockedFileExtensions = new List<string> { ".sh" }
        };
        context.SandboxPolicies.Add(existing);
        await context.SaveChangesAsync();

        var repo = new SandboxPolicyRepository(context);
        var updated = new SandboxPolicy
        {
            Id = existing.Id,
            UserId = "user-2",
            IsReadOnly = true,
            MaxPathLength = 100,
            AllowDotFiles = true,
            BlockedFileExtensions = new List<string> { ".ps1", ".exe" }
        };

        await repo.SetPolicyAsync(updated);

        var stored = await context.SandboxPolicies.SingleAsync();
        stored.IsReadOnly.Should().BeTrue();
        stored.MaxPathLength.Should().Be(100);
        stored.AllowDotFiles.Should().BeTrue();
        stored.BlockedFileExtensions.Should().BeEquivalentTo(new[] { ".ps1", ".exe" });
    }
}
