using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Common;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;


// another test suite for sandbox which checks sandbox rules and sharing sistems ACL, RBAC.

namespace NexusFS.Tests
{
    public class SandboxGuardTests
    {
        private readonly Mock<IAccessControlRepository> _mockAccessControlRepository;
        private readonly Mock<ISandboxPolicyRepository> _mockPolicyRepository;
        private readonly Mock<Logger> _mockLogger;
        private readonly SandboxGuard _sandboxGuard;

        public SandboxGuardTests()
        {
            _mockAccessControlRepository = new Mock<IAccessControlRepository>();
            _mockPolicyRepository = new Mock<ISandboxPolicyRepository>();


            var mockAuditLogRepository = new Mock<IAuditLogRepository>();
            _mockLogger = new Mock<Logger>(mockAuditLogRepository.Object);


            _sandboxGuard = new SandboxGuard(
                _mockAccessControlRepository.Object,
                _mockPolicyRepository.Object,
                _mockLogger.Object
            );
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateAccessAsync_ShouldThrow_WhenUserIdInvalid(string? invalidUserId)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(invalidUserId, "file.txt", FileOperation.Read));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateAccessAsync_ShouldThrow_WhenPathInvalid(string? invalidPath)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync("user1", invalidPath, FileOperation.Read));
        }

        [Theory]
        [InlineData("../etc/passwd")]
        [InlineData("folder/../../secret.txt")]
        [InlineData("..\\windows\\system32")]
        public async Task ValidateAccessAsync_ShouldRejectPathTraversal(string maliciousPath)
        {
            // Arrange
            string userId = "user1";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, maliciousPath, FileOperation.Read));

            Assert.Contains("Path traversal detected", ex.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldBlockWrite_WhenPolicyIsReadOnly()
        {

            var userId = "read_only_user";
            var path = "data.txt";

            // Policy: Read Only Mode
            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy { IsReadOnly = true });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, path, FileOperation.Write));

            Assert.Contains("Read-Only mode", ex.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldAllowRead_WhenPolicyIsReadOnly()
        {

            var userId = "read_only_user";
            var path = "data.txt";

            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy { IsReadOnly = true });

            //ACL allows read
            _mockAccessControlRepository.Setup(a => a.HasAccessAsync(userId, It.IsAny<string>(), "read"))
                .ReturnsAsync(true);

            // Act
            await _sandboxGuard.ValidateAccessAsync(userId, path, FileOperation.Read);

            // Assert: No exception thrown
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldBlockForbiddenExtensions()
        {

            var userId = "script_kiddie";
            var path = "malware.exe"; // Forbidden extension

            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy { BlockedFileExtensions = new List<string> { ".exe", ".bat" } });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, path, FileOperation.Create));

            Assert.Contains("extension '.exe' is not allowed", ex.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldBlockHiddenFiles_WhenDotFilesRestricted()
        {

            var userId = "user1";
            var path = ".env"; // hidden file

            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy { AllowDotFiles = false });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, path, FileOperation.Read));

            Assert.Contains("hidden files", ex.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldBlockLongPaths()
        {
            // Arrange
            var userId = "user1";
            var longPath = new string('a', 300) + ".txt"; // 304 chars

            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy { MaxPathLength = 260 });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, longPath, FileOperation.Write));

            Assert.Contains("exceeds maximum length", ex.Message);
        }


        [Fact]
        public async Task ValidateAccessAsync_ShouldCheckACL_WhenPolicyPasses()
        {

            var userId = "user1";
            var path = "valid.txt";

            // policy passes (default permissive)
            _mockPolicyRepository.Setup(p => p.GetPolicyForUserAsync(userId))
                .ReturnsAsync(new SandboxPolicy());

            // ACL Denies Access
            _mockAccessControlRepository.Setup(a => a.HasAccessAsync(userId, It.IsAny<string>(), "write"))
                .ReturnsAsync(false); // Denied

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sandboxGuard.ValidateAccessAsync(userId, path, FileOperation.Write));

            Assert.Contains("do not have permission", ex.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldNormalizePath_BeforeCheckingACL()
        {

            var userId = "user1";
            var inputPath = "folder\\subfolder\\file.txt"; // Windows style
            string? capturedPath = null;

            _mockAccessControlRepository
                .Setup(a => a.HasAccessAsync(userId, It.IsAny<string>(), "read"))
                .Callback<string, string, string>((u, p, o) => capturedPath = p) // Capture what was sent to repo
                .ReturnsAsync(true);


            await _sandboxGuard.ValidateAccessAsync(userId, inputPath, FileOperation.Read);


            Assert.NotNull(capturedPath);
            Assert.DoesNotContain("\\", capturedPath);
            Assert.Contains("folder/subfolder/file.txt", capturedPath);
        }

        [Theory]
        [InlineData(FileOperation.Read, "read")]
        [InlineData(FileOperation.Write, "write")]
        [InlineData(FileOperation.Delete, "delete")]
        public async Task ValidateAccessAsync_ShouldMapOperationsCorrectly(FileOperation opEnum, string expectedString)
        {

            var userId = "user1";
            string? capturedOp = null;

            _mockAccessControlRepository
                .Setup(a => a.HasAccessAsync(userId, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((u, p, op) => capturedOp = op)
                .ReturnsAsync(true);


            await _sandboxGuard.ValidateAccessAsync(userId, "file.txt", opEnum);


            Assert.Equal(expectedString, capturedOp);
        }


        //SHARE FILE
        [Fact]
        public async Task AccessControl_ShouldEnforceViewerVsEditor()
        {
            // Arrange
            // 1. Setup DB
            var options = new DbContextOptionsBuilder<NexusFSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var context = new NexusFSDbContext(options);
            var repo = new AccessControlRepository(context);

            // 2. Create a file share
            var file = "C:/Shared/Project.doc";
            // Give "Bob" only Viewer access
            await repo.ShareFileAsync(file, "Bob", SharePermission.Viewer);
            // Give "Alice" Editor access
            await repo.ShareFileAsync(file, "Alice", SharePermission.Editor);

            // Act & Assert

            // Bob tries to WRITE -> Should Fail (Viewer < Editor)
            Assert.False(await repo.HasAccessAsync("Bob", file, "write"));

            // Bob tries to READ -> Should Pass
            Assert.True(await repo.HasAccessAsync("Bob", file, "read"));

            // Alice tries to WRITE -> Should Pass (Editor >= Editor)
            Assert.True(await repo.HasAccessAsync("Alice", file, "write"));
        }
    }


}