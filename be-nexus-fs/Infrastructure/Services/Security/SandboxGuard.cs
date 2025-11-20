using Application.Common;
using Infrastructure.Services.Observability;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Proxy Pattern implementation.
    /// Enforces sandbox restrictions and validates resource access.
    /// 
    /// AOP Join Points (placeholders):
    /// - Logging: Log method entry, parameters, exit, results, exceptions
    /// - Metrics: Measure execution time for methods
    /// - ErrorHandling: Capture and log exceptions
    /// - Security/Audit: Track access attempts and failures
    /// </summary>
    public class SandboxGuard
    {
        private readonly IACLManager _aclManager;
        private readonly Logger _logger;

        public SandboxGuard(IACLManager aclManager, Logger logger)
        {
            _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates access for a user to perform an operation on a path.
        /// Normalizes path, checks for path traversal, validates ACL permissions.
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="path">The file path to validate</param>
        /// <param name="operation">The file operation to perform</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied</exception>
        public async Task ValidateAccessAsync(string userId, string path, FileOperation operation)
        {
            try
            {
                _logger.LogInformation($"Validating access for user '{userId}' to path '{path}' for operation '{operation}'", "SandboxGuard");

                // Validate inputs
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Validation failed: userId is null or empty", "SandboxGuard");
                    throw new UnauthorizedAccessException("User ID cannot be null or empty.");
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    _logger.LogWarning($"Validation failed: path is null or empty for user '{userId}'", "SandboxGuard");
                    throw new UnauthorizedAccessException("Path cannot be null or empty.");
                }

                // Check for path traversal attempts BEFORE normalization
                if (path.Contains(".."))
                {
                    _logger.LogWarning($"Path traversal attempt detected for user '{userId}' on path '{path}'", "SandboxGuard");
                    throw new UnauthorizedAccessException($"Path traversal is not allowed: {path}");
                }

                // Normalize path
                string normalizedPath;
                try
                {
                    normalizedPath = Path.GetFullPath(path);
                    normalizedPath = normalizedPath.Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Path normalization failed for path '{path}': {ex.Message}", "SandboxGuard", ex);
                    throw new UnauthorizedAccessException($"Invalid path: {path}", ex);
                }

                _logger.LogDebug($"Normalized path: '{normalizedPath}'", "SandboxGuard");

                // Validate ACL permissions
                bool hasAccess = await _aclManager.HasAccessAsync(userId, normalizedPath, operation);

                if (!hasAccess)
                {
                    _logger.LogWarning($"Access denied for user '{userId}' to path '{normalizedPath}' for operation '{operation}'", "SandboxGuard");
                    throw new UnauthorizedAccessException($"Access denied for user '{userId}' to perform '{operation}' on '{path}'.");
                }

                _logger.LogInformation($"Access granted for user '{userId}' to path '{normalizedPath}' for operation '{operation}'", "SandboxGuard");
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw authorization exceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during access validation for user '{userId}': {ex.Message}", "SandboxGuard", ex);
                throw new UnauthorizedAccessException("Access validation failed due to an internal error.", ex);
            }
        }
    }
}
