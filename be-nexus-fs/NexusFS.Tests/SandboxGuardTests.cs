using System;
using System.Threading.Tasks;
using Application.Common;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Moq;
using Xunit;

namespace NexusFS.Tests
{
    /// <summary>
    /// Unit tests for SandboxGuard.ValidateAccessAsync method.
    /// Tests path normalization, path traversal detection, and ACL validation.
    /// </summary>
    public class SandboxGuardTests
    {
        private readonly Mock<IACLManager> _mockAclManager;
        private readonly Mock<Logger> _mockLogger;
        private readonly SandboxGuard _sandboxGuard;

        public SandboxGuardTests()
        {
            _mockAclManager = new Mock<IACLManager>();
            
            // Mock Logger with required constructor parameter
            var mockAuditLogRepository = new Mock<Domain.Repositories.IAuditLogRepository>();
            _mockLogger = new Mock<Logger>(mockAuditLogRepository.Object);
            
            _sandboxGuard = new SandboxGuard(_mockAclManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldRejectPathTraversal()
        {
            // Arrange
            string userId = "user1";
            string path = "/valid/path/../../../etc/passwd";
            var operation = FileOperation.Read;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sandboxGuard.ValidateAccessAsync(userId, path, operation)
            );

            Assert.Contains("Path traversal is not allowed", exception.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldAllowValidPath_WhenUserHasAccess()
        {
            // Arrange
            string userId = "user1";
            string path = "/valid/path/to/file.txt";
            var operation = FileOperation.Read;

            // Mock ACL to grant access
            _mockAclManager
                .Setup(m => m.HasAccessAsync(userId, It.IsAny<string>(), operation))
                .ReturnsAsync(true);

            // Act
            await _sandboxGuard.ValidateAccessAsync(userId, path, operation);

            // Assert - No exception thrown means success
            _mockAclManager.Verify(
                m => m.HasAccessAsync(userId, It.IsAny<string>(), operation),
                Times.Once
            );
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldThrowException_WhenAccessDenied()
        {
            // Arrange
            string userId = "user1";
            string path = "/restricted/path/file.txt";
            var operation = FileOperation.Write;

            // Mock ACL to deny access
            _mockAclManager
                .Setup(m => m.HasAccessAsync(userId, It.IsAny<string>(), operation))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sandboxGuard.ValidateAccessAsync(userId, path, operation)
            );

            Assert.Contains("Access denied", exception.Message);
            Assert.Contains(userId, exception.Message);
        }

        [Theory]
        [InlineData("C:\\Users\\test\\file.txt")]  // Windows path with backslashes
        [InlineData("./relative/path")]             // Relative path
        [InlineData("file.txt")]                    // Simple filename
        public async Task ValidateAccessAsync_ShouldNormalizePath(string inputPath)
        {
            // Arrange
            string userId = "user1";
            var operation = FileOperation.Read;
            string? capturedPath = null;

            // Mock ACL to grant access and capture normalized path
            _mockAclManager
                .Setup(m => m.HasAccessAsync(userId, It.IsAny<string>(), operation))
                .Callback<string, string, FileOperation>((user, normalizedPath, op) =>
                {
                    capturedPath = normalizedPath;
                })
                .ReturnsAsync(true);

            // Act
            await _sandboxGuard.ValidateAccessAsync(userId, inputPath, operation);

            // Assert - Path should be normalized (no backslashes)
            Assert.NotNull(capturedPath);
            Assert.DoesNotContain("\\", capturedPath);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldThrowException_WhenUserIdIsEmpty()
        {
            // Arrange
            string userId = "";
            string path = "/valid/path/file.txt";
            var operation = FileOperation.Read;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sandboxGuard.ValidateAccessAsync(userId, path, operation)
            );

            Assert.Contains("User ID cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldThrowException_WhenPathIsEmpty()
        {
            // Arrange
            string userId = "user1";
            string path = "";
            var operation = FileOperation.Read;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sandboxGuard.ValidateAccessAsync(userId, path, operation)
            );

            Assert.Contains("Path cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task ValidateAccessAsync_ShouldRejectPathWithDotsAfterNormalization()
        {
            // Arrange
            string userId = "user1";
            // This path will be normalized but still contain ".." in the result
            string path = "../parent/file.txt";
            var operation = FileOperation.Read;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sandboxGuard.ValidateAccessAsync(userId, path, operation)
            );

            // Should detect path traversal after normalization
            Assert.Contains("Path traversal is not allowed", exception.Message);
        }
    }
}
