using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Common; 
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services.Observability;

namespace Infrastructure.Services.Security
{
    public class SandboxGuard
    {
        private readonly IAccessControlRepository _accessControlRepository;
        private readonly ISandboxPolicyRepository _policyRepository;
        private readonly Logger _logger;

        public SandboxGuard(
            IAccessControlRepository accessControlRepository, 
            ISandboxPolicyRepository policyRepository,
            Logger logger)
        {
            _accessControlRepository = accessControlRepository ?? throw new ArgumentNullException(nameof(accessControlRepository));
            _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ValidateAccessAsync(string userId, string path, FileOperation operation)
        {
            try
            {
                _logger.LogInformation($"Validating: User '{userId}' -> '{operation}' -> '{path}'", "SandboxGuard");

              
                if (string.IsNullOrWhiteSpace(path)) throw new UnauthorizedAccessException("Path required.");
                if (path.Contains("..")) 
                {
                    _logger.LogWarning($"Path traversal detected: {path}", "SandboxGuard");
                    throw new UnauthorizedAccessException("Path traversal detected.");
                }

              
                string normalizedPath = Path.GetFullPath(path).Replace("\\", "/");

                // 1. ENFORCE POLICY (Governance)
                // This checks global rules before checking specific file permissions
                
                await EnforcePolicyAsync(userId, normalizedPath, operation);

                // 2. ENFORCE ACL (Permissions)
             
                var operationString = MapOperationToString(operation);
                bool hasAccess = await _accessControlRepository.HasAccessAsync(userId, normalizedPath, operationString);

                if (!hasAccess)
                {
                    _logger.LogWarning($"ACL Denied: {userId} cannot perform '{operation}' on '{normalizedPath}'", "SandboxGuard");
                    throw new UnauthorizedAccessException("You do not have permission to access this resource.");
                }

                _logger.LogInformation("Access Granted", "SandboxGuard");
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError($"Guard Error: {ex.Message}", "SandboxGuard");
                throw new UnauthorizedAccessException("Access denied due to system error.", ex);
            }
        }

        /// <summary>
        /// Validates against the user's Sandbox Policy.
        /// </summary>
        private async Task EnforcePolicyAsync(string userId, string path, FileOperation operation)
        {
            // Fetch policy, default to empty if none exists
            var policy = await _policyRepository.GetPolicyForUserAsync(userId) ?? new SandboxPolicy();

            // Rule 1: Read-Only Mode
            // If policy says ReadOnly, and user tries to Write/Delete -> Block
            if (policy.IsReadOnly && IsWriteOperation(operation))
            {
                _logger.LogWarning($"Policy Violation: User '{userId}' attempted write in Read-Only mode.", "SandboxGuard");
                throw new UnauthorizedAccessException("Sandbox is in Read-Only mode.");
            }

            // Rule 2: Path Length
            if (path.Length > policy.MaxPathLength)
            {
                throw new UnauthorizedAccessException($"Path exceeds maximum length of {policy.MaxPathLength}.");
            }

            // Rule 3: Hidden Files (Dot-files)
            var fileName = Path.GetFileName(path);
            if (!policy.AllowDotFiles && fileName.StartsWith("."))
            {
                _logger.LogWarning($"Policy Violation: Dot-file access denied '{fileName}'", "SandboxGuard");
                throw new UnauthorizedAccessException("Access to hidden files is restricted.");
            }

            // Rule 4: Blocked Extensions (Only relevant for Write/Create)
            if (IsWriteOperation(operation))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (policy.BlockedFileExtensions.Contains(ext))
                {
                    _logger.LogWarning($"Policy Violation: Blocked extension '{ext}'", "SandboxGuard");
                    throw new UnauthorizedAccessException($"File extension '{ext}' is not allowed.");
                }
            }
        }

        private bool IsWriteOperation(FileOperation op) 
            => op is FileOperation.Write or FileOperation.Create or FileOperation.Delete or FileOperation.Move or FileOperation.Copy;

        private static string MapOperationToString(FileOperation operation)
        {
            return operation switch
            {
                FileOperation.Read => "read",
                FileOperation.Write => "write",
                FileOperation.Delete => "delete",
                FileOperation.List => "list",
                FileOperation.Create => "create",
                FileOperation.Move => "move",
                FileOperation.Copy => "copy",
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };
        }
    }
}