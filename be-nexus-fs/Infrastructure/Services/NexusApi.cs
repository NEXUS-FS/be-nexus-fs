using Application.DTOs;
using Application.Common;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;

namespace Infrastructure.Services
{
    /// <summary>
    /// Central entry point that exposes the main file operations to the client or UI layer.
    /// AOP Concerns: Logging, Authentication, Metrics, Error Handling
    /// </summary>
    public class NexusApi
    {
        private readonly ProviderRouter _providerRouter;
        private readonly MCPServerProxy _mcpServerProxy;
        private readonly Logger _logger;

        public NexusApi(ProviderRouter providerRouter, MCPServerProxy mcpServerProxy, Logger logger)
        {
            _providerRouter = providerRouter;
            _mcpServerProxy = mcpServerProxy;
            _logger = logger;
        }

        /// <summary>
        /// Reads a file from the specified provider.
        /// Wrapped with MCPServerProxy for security validation and audit logging.
        /// </summary>
        /// <param name="providerId">The provider identifier</param>
        /// <param name="filePath">The file path to read</param>
        /// <param name="userId">The user requesting the operation</param>
        /// <returns>FileOperationResponse with file content</returns>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit with result
        // [AOP: AuthenticationAspect] BEFORE: Validate user authentication and authorization
        // [AOP: MetricsAspect] AROUND: Measure execution time and count requests
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture, log, and wrap exceptions
        public async Task<FileOperationResponse> ReadFile(string providerId, string filePath, string userId)
        {
            return await _mcpServerProxy.ExecuteSecure(
                FileOperation.Read,
                providerId,
                filePath,
                userId,
                async () =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "filePath", filePath }
                    };
                    return await _providerRouter.ExecuteOperation(providerId, "Read", parameters);
                });
        }

        /// <summary>
        /// Writes content to a file in the specified provider.
        /// Wrapped with MCPServerProxy for security validation and audit logging.
        /// </summary>
        /// <param name="providerId">The provider identifier</param>
        /// <param name="filePath">The file path to write</param>
        /// <param name="content">The content to write</param>
        /// <param name="userId">The user requesting the operation</param>
        /// <returns>FileOperationResponse with operation status</returns>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit with result
        // [AOP: AuthenticationAspect] BEFORE: Validate user authentication and authorization
        // [AOP: MetricsAspect] AROUND: Measure execution time and count requests
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture, log, and wrap exceptions
        public async Task<FileOperationResponse> WriteFile(string providerId, string filePath, string content, string userId)
        {
            return await _mcpServerProxy.ExecuteSecure(
                FileOperation.Write,
                providerId,
                filePath,
                userId,
                async () =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "filePath", filePath },
                        { "content", content }
                    };
                    return await _providerRouter.ExecuteOperation(providerId, "Write", parameters);
                });
        }

        /// <summary>
        /// Deletes a file from the specified provider.
        /// Wrapped with MCPServerProxy for security validation and audit logging.
        /// </summary>
        /// <param name="providerId">The provider identifier</param>
        /// <param name="filePath">The file path to delete</param>
        /// <param name="userId">The user requesting the operation</param>
        /// <returns>FileOperationResponse with operation status</returns>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit with result
        // [AOP: AuthenticationAspect] BEFORE: Validate user authentication and authorization
        // [AOP: MetricsAspect] AROUND: Measure execution time and count requests
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture, log, and wrap exceptions
        public async Task<FileOperationResponse> DeleteFile(string providerId, string filePath, string userId)
        {
            return await _mcpServerProxy.ExecuteSecure(
                FileOperation.Delete,
                providerId,
                filePath,
                userId,
                async () =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "filePath", filePath }
                    };
                    return await _providerRouter.ExecuteOperation(providerId, "Delete", parameters);
                });
        }
    }
}

// Example of implementation that will be added next laboratories
// namespace Infrastructure.Services
// {
//     public class NexusApi
//     {
//         // Apply logging aspect to specific methods
//         [LoggingAspect]
//         public async Task<FileOperationResponse> ReadFile(string providerId, string filePath)
//         {
//             // Business logic - logging is automatically applied
//             return await ProcessFileOperation("READ", providerId, filePath);
//         }
//
//         [LoggingAspect]
//         public async Task<FileOperationResponse> WriteFile(string providerId, string filePath, string content)
//         {
//             return await ProcessFileOperation("WRITE", providerId, filePath, content);
//         }
//
//         [LoggingAspect]
//         public async Task<FileOperationResponse> DeleteFile(string providerId, string filePath)
//         {
//             return await ProcessFileOperation("DELETE", providerId, filePath);
//         }
//     }
// }
