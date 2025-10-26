namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Proxy Pattern implementation.
    /// Enforces sandbox restrictions and validates resource access.
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
            _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
            StrategyName = strategyName ?? throw new ArgumentNullException(nameof(strategyName));
        }

        /// <summary>
        /// Validates access for a given user and resource.
        /// Combines ACL and sandbox path validation.
        /// </summary>
        public async Task<bool> ValidateAccessAsync(string username, string resourcePath, string permission)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(resourcePath) ||
                string.IsNullOrWhiteSpace(permission))
                return false;

            bool hasPermission = await Task.Run(() => _aclManager.HasPermission(username, permission));
            bool isAllowed = await IsPathAllowedAsync(resourcePath);

            return hasPermission && isAllowed;
        }

        /// <summary>
        /// Checks if a given path is within the sandbox’s allowed boundaries.
        /// </summary>
        public Task<bool> IsPathAllowedAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult(false);

            lock (_lock)
            {
                return Task.FromResult(_allowedPaths.Contains(path));
            }
        }

        /// <summary>
        /// Adds a new path to the list of allowed sandbox paths.
        /// </summary>
        public Task AddAllowedPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            lock (_lock)
            {
                _allowedPaths.Add(path);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a path from the list of allowed sandbox paths.
        /// </summary>
        public Task RemoveAllowedPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            lock (_lock)
            {
                _allowedPaths.Remove(path);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns all allowed paths for this sandbox.
        /// </summary>
        public Task<IEnumerable<string>> GetAllowedPathsAsync()
        {
            lock (_lock)
            {
                var copy = _allowedPaths.ToList();
                return Task.FromResult<IEnumerable<string>>(copy);
            }
        }

        /// <summary>
        /// Enforces sandbox restrictions before executing an operation.
        /// Throws an exception if access is denied.
        /// </summary>
        public async Task EnforceSandboxAsync(string username, string resourcePath, string permission)
        {
            bool accessGranted = await ValidateAccessAsync(username, resourcePath, permission);

            if (!accessGranted)
                throw new UnauthorizedAccessException(
                    $"Access denied for user '{username}' to '{resourcePath}' in sandbox '{StrategyName}'.");
        }
    }
}