using System.Collections.Concurrent;
using Application.Common;
using Domain.Repositories;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Manages user permissions and access control lists.
    /// Uses in-memory caching with persistent storage via repository.
    /// </summary>
    public class ACLManager : IACLManager
    {
        private readonly IAccessControlRepository _repository;
        
        // Thread-safe dictionary to cache user permissions at runtime
        // Key: username (case-insensitive), Value: set of permissions
        private readonly ConcurrentDictionary<string, HashSet<string>> _userPermissions;
        
        public ACLManager(IAccessControlRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userPermissions = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            
            // Load permissions from repository at startup
            _ = InitializePermissionsAsync();
        }

        /// <summary>
        /// Loads all permissions from persistent storage into memory cache.
        /// </summary>
        private async Task InitializePermissionsAsync()
        {
            try
            {
                var allPermissions = await _repository.GetAllPermissionsAsync();
                
                foreach (var (username, permissions) in allPermissions)
                {
                    _userPermissions[username] = new HashSet<string>(
                        permissions, 
                        StringComparer.OrdinalIgnoreCase
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error - permissions will be loaded on-demand
                Console.WriteLine($"Error loading permissions: {ex.Message}");
            }
        }

        /// <summary>
        /// Grants a permission to a user. Persists to repository and updates cache.
        /// </summary>
        public async Task GrantPermissionAsync(string username, string permission)
        {
            Validate(username, permission);

            // Persist to repository first
            var success = await _repository.AddPermissionAsync(username, permission);
            
            if (success)
            {
                // Update in-memory cache
                _userPermissions.AddOrUpdate(
                    username,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { permission },
                    (key, existing) =>
                    {
                        existing.Add(permission);
                        return existing;
                    }
                );
            }
            else
            {
                throw new InvalidOperationException($"Failed to grant permission '{permission}' to user '{username}'");
            }
        }

        /// <summary>
        /// Revokes a permission from a user. Persists to repository and updates cache.
        /// </summary>
        public async Task RevokePermissionAsync(string username, string permission)
        {
            Validate(username, permission);

            // Persist to repository first
            var success = await _repository.RemovePermissionAsync(username, permission);
            
            if (success && _userPermissions.TryGetValue(username, out var permissions))
            {
                // Update in-memory cache
                permissions.Remove(permission);
                
                // Remove user entry if no permissions left
                if (permissions.Count == 0)
                {
                    _userPermissions.TryRemove(username, out _);
                }
            }
        }

        /// <summary>
        /// Checks if a user has a specific permission.
        /// First checks cache, then falls back to repository if not found.
        /// </summary>
        public async Task<bool> HasPermissionAsync(string username, string permission)
        {
            Validate(username, permission);

            // Check in-memory cache first
            if (_userPermissions.TryGetValue(username, out var permissions) && 
                permissions.Contains(permission))
            {
                return true;
            }

            // Cache miss - check repository and update cache
            var hasPermission = await _repository.HasPermissionAsync(username, permission);
            
            if (hasPermission)
            {
                // Update cache with found permission
                _userPermissions.AddOrUpdate(
                    username,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { permission },
                    (key, existing) =>
                    {
                        existing.Add(permission);
                        return existing;
                    }
                );
            }

            return hasPermission;
        }

        /// <summary>
        /// Gets all permissions for a user from repository.
        /// </summary>
        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Array.Empty<string>();

            // Check cache first
            if (_userPermissions.TryGetValue(username, out var cachedPermissions))
            {
                return cachedPermissions.ToList();
            }

            // Fetch from repository and update cache
            var permissions = await _repository.GetUserPermissionsAsync(username);
            var permissionSet = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
            
            if (permissionSet.Count > 0)
            {
                _userPermissions[username] = permissionSet;
            }

            return permissions;
        }

        /// <summary>
        /// Revokes all permissions for a user.
        /// </summary>
        public async Task RevokeAllPermissionsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));

            await _repository.RemoveAllPermissionsAsync(username);
            _userPermissions.TryRemove(username, out _);
        }

        /// <summary>
        /// Clears the in-memory cache and reloads from repository.
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            _userPermissions.Clear();
            await InitializePermissionsAsync();
        }

        /// <summary>
        /// Checks if a user has access to perform a specific operation on a resource path.
        /// Maps FileOperation to permission strings and delegates to HasPermissionAsync.
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="path">The resource path being accessed</param>
        /// <param name="operation">The file operation being performed</param>
        /// <returns>True if user has access, false otherwise</returns>
        public async Task<bool> HasAccessAsync(string userId, string path, FileOperation operation)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            // Map FileOperation to permission string
            var permission = MapOperationToPermission(operation);
            
            // Check if user has the required permission
            return await HasPermissionAsync(userId, permission);
        }

        /// <summary>
        /// Maps a FileOperation to its corresponding permission string.
        /// </summary>
        private static string MapOperationToPermission(FileOperation operation)
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
                _ => throw new ArgumentException($"Unknown operation: {operation}", nameof(operation))
            };
        }

        private static void Validate(string username, string permission)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));
        }
    }
}