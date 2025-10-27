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
        private readonly ACLManager _aclManager;

        // Defines the sandbox strategy name (different strategies = different restriction rules)
        public string StrategyName { get; }

        // Allowed paths inside this sandbox ..
        private readonly HashSet<string> _allowedPaths = new(StringComparer.OrdinalIgnoreCase);

        // Thread-safety lock object
        private readonly object _lock = new();

        // Constructor that I forgot..
        public SandboxGuard(ACLManager aclManager, string strategyName)
        {
            // [AOP: Logging] BEFORE: Log constructor call and parameters
            _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
            StrategyName = strategyName ?? throw new ArgumentNullException(nameof(strategyName));
            // [AOP: Metrics] AFTER: Track SandboxGuard instantiation
        }

        /// <summary>
        /// Validates access for a given user and resource.
        /// Combines ACL and sandbox path validation.
        /// </summary>
        public async Task<bool> ValidateAccessAsync(string username, string resourcePath, string permission)
        {

            // [AOP: Logging] BEFORE: Log method entry and input parameters
            // [AOP: Metrics] AROUND: Measure execution time
            // [AOP: Security/Audit] BEFORE: Track access attempt

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(resourcePath) ||
                string.IsNullOrWhiteSpace(permission))
                return false;

            bool hasPermission = await Task.Run(() => _aclManager.HasPermission(username, permission));
            bool isAllowed = await IsPathAllowedAsync(resourcePath);

            // [AOP: Logging] AFTER: Log method exit and result
            return hasPermission && isAllowed;
        }

        /// <summary>
        /// Checks if a given path is within the sandbox’s allowed boundaries.
        /// </summary>
        public Task<bool> IsPathAllowedAsync(string path)
        {
            // [AOP: Logging] BEFORE: Log path check
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult(false);

            lock (_lock)
            {   // [AOP: Logging] AFTER: Log path check result
                return Task.FromResult(_allowedPaths.Contains(path));
            }
        }

        /// <summary>
        /// Adds a new path to the list of allowed sandbox paths.
        /// </summary>
        public Task AddAllowedPathAsync(string path)
        {
            // [AOP: Logging] BEFORE: Log path addition attempt
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            lock (_lock)
            {
                _allowedPaths.Add(path);
            }
            // [AOP: Logging] AFTER: Log successful path addition
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a path from the list of allowed sandbox paths.
        /// </summary>
        public Task RemoveAllowedPathAsync(string path)
        {
            // [AOP: Logging] BEFORE: Log path removal attempt
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            lock (_lock)
            {
                _allowedPaths.Remove(path);
            }
            // [AOP: Logging] AFTER: Log successful path removal
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns all allowed paths for this sandbox.
        /// </summary>
        public Task<IEnumerable<string>> GetAllowedPathsAsync()
        { // [AOP: Logging] BEFORE: Log retrieval attempt
            lock (_lock)
            {
                var copy = _allowedPaths.ToList();
                // [AOP: Logging] AFTER: Log number of allowed paths retrieved
                return Task.FromResult<IEnumerable<string>>(copy);
            }
        }

        /// <summary>
        /// Enforces sandbox restrictions before executing an operation.
        /// Throws an exception if access is denied.
        /// </summary>
        public async Task EnforceSandboxAsync(string username, string resourcePath, string permission)
        {
            // [AOP: Logging] BEFORE: Log enforcement attempt
            // [AOP: Metrics] AROUND: Measure enforcement execution time
            // [AOP: Security/Audit] BEFORE: Track enforcement attempt
            bool accessGranted = await ValidateAccessAsync(username, resourcePath, permission);

            if (!accessGranted)
                // [AOP: ErrorHandling] AFTER_THROWING: Log unauthorized access
                throw new UnauthorizedAccessException(
                    $"Access denied for user '{username}' to '{resourcePath}' in sandbox '{StrategyName}'.");
            // [AOP: Logging] AFTER: Log successful enforcement
        }
    }
}