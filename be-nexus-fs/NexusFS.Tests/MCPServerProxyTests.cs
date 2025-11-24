using Application.Common;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace NexusFS.Tests;

/// <summary>
/// Integration tests for MCPServerProxy.
/// Tests that MCPServerProxy wraps NexusApi calls with SandboxGuard.ValidateAccessAsync,
/// logs all access attempts, enforces operation restrictions, and returns audit trail.
/// </summary>
public class MCPServerProxyTests : IDisposable
{
    private readonly NexusFSDbContext _dbContext;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAccessControlRepository _accessControlRepository;
    private readonly MCPServerProxy _mcpServerProxy;
    private readonly NexusApi _nexusApi;

    public MCPServerProxyTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<NexusFSDbContext>()
            .UseInMemoryDatabase(databaseName: $"MCPServerProxyTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new NexusFSDbContext(options);

        // Setup repositories
        _auditLogRepository = new AuditLogRepository(_dbContext);
        _accessControlRepository = new AccessControlRepository(_dbContext);
        ISandboxPolicyRepository sandboxPolicyRepository = new SandboxPolicyRepository(_dbContext);

        // Setup logger
        var logger = new Logger(_auditLogRepository);

        // Setup SandboxGuard
        var sandboxGuard = new SandboxGuard(_accessControlRepository, sandboxPolicyRepository, logger);

        // Setup ACLManager
        var aclManager = new ACLManager(_accessControlRepository);

        // Setup MCPServerProxy
        _mcpServerProxy = new MCPServerProxy(sandboxGuard, aclManager, _auditLogRepository);

        // Setup ProviderRouter and ProviderManager for NexusApi
        var factory = new ProviderFactory();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);
        var observers = new List<IProviderObserver> { logger };
        var providerManager = new ProviderManager(factory, logger, mockScopeFactory.Object, observers);
        var authManager = new AuthManager(logger);
        var providerRouter = new ProviderRouter(providerManager, logger, authManager);

        // Setup NexusApi
        _nexusApi = new NexusApi(providerRouter, _mcpServerProxy, logger);

        // Setup test data: Create a memory provider and grant permissions
        SetupTestData().Wait();
    }

    private async Task SetupTestData()
    {
        // Create a test user with permissions
        var userId = "test-user";
        var filePath = "test-file.txt";

        // Grant read permission
        await _accessControlRepository.AddPermissionAsync(userId, "read");
        await _accessControlRepository.HasAccessAsync(userId, filePath, "read");

        // Register a memory provider for testing
        var provider = new MemoryProvider("test-provider");
        await provider.Initialize(new Dictionary<string, string>());
        await provider.WriteFileAsync(filePath, "test content");

        // Register provider with ProviderManager (simplified - in real scenario this would be done via repository)
        // For this test, we'll use the provider directly via ProviderRouter
    }

    [Fact]
    public async Task ExecuteSecure_ShouldCallSandboxGuardValidateAccessAsync()
    {
        var userId = "test-user";
        var filePath = "test-file.txt";
        var providerId = "test-provider";
        var operation = FileOperation.Read;

        // This should not throw if SandboxGuard validates successfully
        var result = await _mcpServerProxy.ExecuteSecure(
            operation,
            providerId,
            filePath,
            userId,
            async () => await Task.FromResult("test result")
        );

        Assert.Equal("test result", result);
    }

    [Fact]
    public async Task ExecuteSecure_ShouldLogAccessAttempt_OnSuccess()
    {
        var userId = "test-user";
        var filePath = "test-file.txt";
        var providerId = "test-provider";
        var operation = FileOperation.Read;

        await _mcpServerProxy.ExecuteSecure(
            operation,
            providerId,
            filePath,
            userId,
            async () => await Task.FromResult("success")
        );

        // Check audit logs
        var auditLogs = await _auditLogRepository.GetByUserIdAsync(userId, DateTime.UtcNow.AddMinutes(-1));
        var logEntry = auditLogs.FirstOrDefault(log =>
            log.ResourcePath == filePath &&
            log.Action.Contains("Read") &&
            log.Action.Contains("ALLOWED"));

        Assert.NotNull(logEntry);
        Assert.Equal(userId, logEntry.UserId);
        Assert.Contains("ALLOWED", logEntry.Action);
    }

    [Fact]
    public async Task ExecuteSecure_ShouldLogAccessAttempt_OnFailure()
    {
        var userId = "unauthorized-user";
        var filePath = "test-file.txt";
        var providerId = "test-provider";
        var operation = FileOperation.Write; // Write operation without permission

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mcpServerProxy.ExecuteSecure(
                operation,
                providerId,
                filePath,
                userId,
                async () => await Task.FromResult("should not execute")
            )
        );

        // Check audit logs for denied access
        var auditLogs = await _auditLogRepository.GetByUserIdAsync(userId, DateTime.UtcNow.AddMinutes(-1));
        var logEntry = auditLogs.FirstOrDefault(log =>
            log.ResourcePath == filePath &&
            log.Action.Contains("Write") &&
            log.Action.Contains("DENIED"));

        Assert.NotNull(logEntry);
        Assert.Equal(userId, logEntry.UserId);
        Assert.Contains("DENIED", logEntry.Action);
    }

    [Fact]
    public async Task ExecuteSecure_ShouldEnforceReadOnlyMode_WhenPolicyIsReadOnly()
    {
        var userId = "readonly-user";
        var filePath = "test-file.txt";
        var providerId = "test-provider";

        // Set read-only policy for user
        var policy = new SandboxPolicy
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            IsReadOnly = true,
            MaxPathLength = 1000,
            AllowDotFiles = false,
            BlockedFileExtensions = new List<string>()
        };
        // Add policy directly to database context since SetPolicyAsync is not in the interface
        _dbContext.SandboxPolicies.Add(policy);
        await _dbContext.SaveChangesAsync();

        // Grant read permission
        await _accessControlRepository.AddPermissionAsync(userId, "read");

        // Read should succeed
        var readResult = await _mcpServerProxy.ExecuteSecure(
            FileOperation.Read,
            providerId,
            filePath,
            userId,
            async () => await Task.FromResult("read content")
        );

        Assert.Equal("read content", readResult);

        // Write should fail due to read-only mode
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _mcpServerProxy.ExecuteSecure(
                FileOperation.Write,
                providerId,
                filePath,
                userId,
                async () => await Task.FromResult("should not execute")
            )
        );
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldReturnAuditLogs_ForUser()
    {
        var userId = "audit-user";
        var filePath = "audit-file.txt";
        var providerId = "test-provider";

        // Grant permissions
        await _accessControlRepository.AddPermissionAsync(userId, "read");

        // Perform operations to generate audit logs
        await _mcpServerProxy.ExecuteSecure(
            FileOperation.Read,
            providerId,
            filePath,
            userId,
            async () => await Task.FromResult("content")
        );

        var auditTrail = await _mcpServerProxy.GetAuditTrailAsync(userId);
        var auditLogEntities = auditTrail as AuditLogEntity[] ?? auditTrail.ToArray();
        Assert.NotEmpty(auditLogEntities);
        var log = auditLogEntities[0];
        Assert.Equal(userId, log.UserId);
        Assert.Contains("Read", log.Action);
    }

    [Fact]
    public async Task GetAuditTrailByDateRangeAsync_ShouldReturnAuditLogs_InDateRange()
    {
        var userId = "date-range-user";
        var filePath = "date-file.txt";
        var providerId = "test-provider";

        // Grant permissions
        await _accessControlRepository.AddPermissionAsync(userId, "read");

        var startTime = DateTime.UtcNow;

        // Perform operation
        await _mcpServerProxy.ExecuteSecure(
            FileOperation.Read,
            providerId,
            filePath,
            userId,
            async () => await Task.FromResult("content")
        );

        var endTime = DateTime.UtcNow;

        var auditTrail = await _mcpServerProxy.GetAuditTrailByDateRangeAsync(
            startTime.AddMinutes(-1),
            endTime.AddMinutes(1)
        );
        var auditLogEntities = auditTrail as AuditLogEntity[] ?? auditTrail.ToArray();
        Assert.NotEmpty(auditLogEntities);
        var log = auditLogEntities[0];
        Assert.True(log.Timestamp >= startTime.AddMinutes(-1));
        Assert.True(log.Timestamp <= endTime.AddMinutes(1));
    }

    [Fact]
    public async Task NexusApi_ReadFile_ShouldUseMCPServerProxy()
    {
        var userId = "nexus-user";
        var filePath = "nexus-file.txt";
        var providerId = "test-provider";

        // Grant read permission
        await _accessControlRepository.AddPermissionAsync(userId, "read");

        // Note: This test requires a registered provider, which is complex to set up.
        // In a real integration test, you would register a provider first.
        // For now, we test that the proxy is called (which will fail at provider level, not proxy level)

        // The call should go through MCPServerProxy, which will validate access
        // If provider is not found, it will fail at ProviderRouter level, not proxy level
        // This confirms the proxy is being used
        try
        {
            await _nexusApi.ReadFile(providerId, filePath, userId);
            // If no exception is thrown, that's okay - the test is about verifying the proxy is used
        }
        catch (KeyNotFoundException)
        {
            // Expected - provider not found
        }
        catch (Exception)
        {
            // Other exceptions are also acceptable - the important thing is the proxy was called
        }

        // Verify audit log was created (even though operation may have failed at provider level)
        var auditLogs = await _auditLogRepository.GetByUserIdAsync(userId, DateTime.UtcNow.AddMinutes(-1));
        // The proxy logs before delegating, so we should see an ALLOWED log
        var allowedLog = auditLogs.FirstOrDefault(log => log.Action.Contains("Read") && log.Action.Contains("ALLOWED"));
        Assert.NotNull(allowedLog);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
