using Application.Common;
using Domain.Entities;
using Domain.Repositories;


namespace Infrastructure.Services.Security;

/// <summary>
/// Proxy Pattern implementation.
/// Validates access and authorizes requests before delegating execution back to NexusApi.
/// The proxy's role is strictly validation and authorization, not business logic execution.
/// </summary>
public class MCPServerProxy
{
    private readonly SandboxGuard _sandboxGuard;
    private readonly ACLManager _aclManager;
    private readonly IAuditLogRepository _auditLogRepository;

    public MCPServerProxy(
        SandboxGuard sandboxGuard,
        ACLManager aclManager,
        IAuditLogRepository auditLogRepository)
    {
        _sandboxGuard = sandboxGuard ?? throw new ArgumentNullException(nameof(sandboxGuard));
        _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    /// <summary>
    /// Generic secure execution method that validates and authorizes any operation before execution.
    /// Wraps any action with security validation and delegates execution back to the caller.
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation type</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="filePath">The file path being accessed</param>
    /// <param name="userId">The user requesting the operation</param>
    /// <param name="action">The actual operation to execute after validation</param>
    /// <returns>The result of the executed action</returns>
    public async Task<T> ExecuteSecure<T>(
        FileOperation operation,
        string providerId,
        string filePath,
        string userId,
        Func<Task<T>> action)
    {
        // Validate operation through sandbox and ACL checks
        bool isAuthorized = await ValidateOperation(operation, providerId, filePath, userId);

        if (!isAuthorized)
        {
            await LogSecurityEvent(operation, providerId, filePath, userId, false);
            throw new UnauthorizedAccessException(
                $"Access denied for operation '{operation}' on '{filePath}' for user '{userId}'");
        }

        try
        {
            // Log successful authorization
            await LogSecurityEvent(operation, providerId, filePath, userId, true);

            // Delegate execution back to the caller (typically NexusApi)
            return await action();
        }
        catch (Exception)
        {
            // Log security-related failures
            await LogSecurityEvent(operation, providerId, filePath, userId, false, "FAILED");
            throw;
        }
    }

    /// <summary>
    /// Validates if the operation is allowed by sandbox rules and ACL permissions.
    /// Uses SandboxGuard.ValidateAccessAsync which enforces both policy and ACL checks.
    /// </summary>
    /// <param name="operation">The operation type</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="filePath">The file path</param>
    /// <param name="userId">The user requesting the operation</param>
    /// <returns>True if operation is allowed, false otherwise</returns>
    private async Task<bool> ValidateOperation(FileOperation operation, string providerId, string filePath,
        string userId)
    {
        try
        {
            // SandboxGuard.ValidateAccessAsync enforces both sandbox policy (read-only mode, etc.) 
            // and ACL permissions. It throws UnauthorizedAccessException if access is denied.
            await _sandboxGuard.ValidateAccessAsync(userId, filePath, operation);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Access denied by SandboxGuard (policy violation or ACL check failed)
            return false;
        }
    }

    /// <summary>
    /// Logs security-related events for audit purposes using IAuditLogRepository.
    /// Logs all access attempts (success/failure) as required.
    /// </summary>
    /// <param name="operation">The operation type</param>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="filePath">The file path</param>
    /// <param name="userId">The user identifier</param>
    /// <param name="allowed">Whether the operation was allowed</param>
    /// <param name="suffix">Optional suffix to append to the operation name (e.g., "FAILED")</param>
    private async Task LogSecurityEvent(FileOperation operation, string providerId, string filePath, string userId,
        bool allowed, string suffix = "")
    {
        try
        {
            string operationName = string.IsNullOrEmpty(suffix) ? operation.ToString() : $"{operation}_{suffix}";
            string status = allowed ? "ALLOWED" : "DENIED";

            var auditLog = new AuditLogEntity
            {
                Id = Guid.NewGuid().ToString(),
                Action = $"{operationName}_{status}",
                ResourcePath = filePath,
                UserId = userId,
                Details = $"Operation: {operationName} | Provider: {providerId} | Status: {status}",
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
        }
        catch (Exception ex)
        {
            // Log to console as fallback if audit repository fails
            Console.WriteLine($"[SECURITY AUDIT ERROR] Failed to log audit event: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns audit trail for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier</param>
    /// <param name="since">Get logs since this date</param>
    /// <returns>Collection of audit log entries</returns>
    public async Task<IEnumerable<AuditLogEntity>> GetAuditTrailAsync(string userId, DateTime? since = null)
    {
        if (since.HasValue)
        {
            return await _auditLogRepository.GetByUserIdAsync(userId, since.Value);
        }

        // Get recent logs if no date specified
        var allLogs = await _auditLogRepository.GetRecentAsync(100);
        return allLogs.Where(log => log.UserId == userId);
    }

    /// <summary>
    /// Returns audit trail for a date range.
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <returns>Collection of audit log entries</returns>
    public async Task<IEnumerable<AuditLogEntity>> GetAuditTrailByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _auditLogRepository.GetByDateRangeAsync(from, to);
    }
}
