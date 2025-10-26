/// <summary>
/// Manages user permissions and access control lists.
/// </summary>

namespace Infrastructure.Services.Security
{
    public class ACLManager
    {
        // Dictionary to store user permissions.
        // Key: username (case-insensitive ?), Value: set of permissions (case-insensitive)
        private readonly Dictionary<string, HashSet<string>> _userPermissions = 
            new(StringComparer.OrdinalIgnoreCase);

        // Thread-safety lock object
        private readonly object _lock = new();

        // Grant permission to user.
        public void GrantPermission(string username, string permission)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));

            lock (_lock)
            {
                if (!_userPermissions.ContainsKey(username))
                {
                    _userPermissions[username] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                _userPermissions[username].Add(permission);
            }
        }
       
        // Revokes a permission from a user
        public void RevokePermission(string username, string permission)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));

            lock (_lock)
            {
                if (_userPermissions.ContainsKey(username))
                {
                    _userPermissions[username].Remove(permission);
                }
            }
        }

        // Checks if user has a given permission.
        public bool HasPermission(string username, string permission)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(permission))
                return false;

            lock (_lock)
            {
                return _userPermissions.TryGetValue(username, out var permissions)
                       && permissions.Contains(permission);
            }
        }
       
        // Gets all permissions from a user
        public IEnumerable<string> GetUserPermissions(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Array.Empty<string>();

            lock (_lock)
            {
                if (_userPermissions.TryGetValue(username, out var permissions))
                {
                    return permissions.ToList(); // Return a copy to avoid external modifications
                }

                return Array.Empty<string>();
            }
        }
    }
}
