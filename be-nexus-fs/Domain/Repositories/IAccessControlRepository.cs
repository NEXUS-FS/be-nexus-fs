namespace Domain.Repositories
{
    /// <summary>
    /// Repository interface for managing access control permissions.
    /// </summary>
    public interface IAccessControlRepository
    {
        /// <summary>
        /// Adds a permission for a user.
        /// </summary>
        Task<bool> AddPermissionAsync(string username, string permission);

        /// <summary>
        /// Removes a specific permission from a user.
        /// </summary>
        Task<bool> RemovePermissionAsync(string username, string permission);

        /// <summary>
        /// Removes all permissions for a user.
        /// </summary>
        Task<bool> RemoveAllPermissionsAsync(string username);

        /// <summary>
        /// Checks if a user has a specific permission.
        /// </summary>
        Task<bool> HasPermissionAsync(string username, string permission);

        /// <summary>
        /// Gets all permissions for a specific user.
        /// </summary>
        Task<IEnumerable<string>> GetUserPermissionsAsync(string username);

        /// <summary>
        /// Gets all permissions for all users (for cache initialization).
        /// Returns dictionary where key is username, value is list of permissions.
        /// </summary>
        Task<Dictionary<string, List<string>>> GetAllPermissionsAsync();

        /// <summary>
        /// Checks if a user has access to perform a specific operation on a resource path.
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="path">The resource path being accessed</param>
        /// <param name="operation">The operation name (e.g., "read", "write", "delete")</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasAccessAsync(string userId, string path, string operation);
    }
}
