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
    }
}
