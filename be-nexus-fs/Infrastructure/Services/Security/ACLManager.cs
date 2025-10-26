/// <summary>
/// Manages user permissions and access control lists.
/// </summary>

namespace Infrastructure.Services.Security
{
    private readonly IAccessControlRepository _accessControlRepository; //consider this has the methods for persistence .. not it don't exist yet...
    

     private readonly Dictionary<string, HashSet<string>> _userPermissions =
            new(StringComparer.OrdinalIgnoreCase);
     public ACLManager(IAccessControlRepository repository)
    {
        _repository = repository;

        //now fill the dictionary from repository at startup...
        //to be impplemented.
    }
        // Dictionary to store user permissions.
        // Key: username (case-insensitive ?), Value: set of permissions (case-insensitive)
       
        // Thread-safety lock object
        private readonly object _lock = new();

        // Grant permission to user.
        public async void GrantPermission(string username, string permission)
        {
            Validate(username, permission);
            lock (_lock)
        {
                //we should use here the repository to persist the changes..
                //and let this in-memory dictionary as a cache at runtime.
                //// -- if( repository.AddPermission(username, permission)) then the other...
                if (!_userPermissions.ContainsKey(username))
                {
                    _userPermissions[username] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                _userPermissions[username].Add(permission);
            }
        }
       
        // Revokes a permission from a user
        public async void RevokePermission(string username, string permission)
        {
            Validate(username, permission);
            lock (_lock)
            {
                if (_userPermissions.ContainsKey(username))
                {
                    _userPermissions[username].Remove(permission);
                }
            }
        }

        // Checks if user has a given permission.
        public async bool HasPermission(string username, string permission)
        {
            Validate(username, permission);

        //here we could verify in the dictionary and if not found, check the repository for persistence...
        lock (_lock)
        {
            return _userPermissions.TryGetValue(username, out var permissions)
                   && permissions.Contains(permission);
        }
        ///repository.HasPermission(username, permission);...
        }

    // Gets all permissions from a user
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Array.Empty<string>();

       // return await _accessControlRepository.GetUserPermissionsAsync(username); //persistence check
    }
        

           private static void Validate(string username, string permission)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));
        }
    }
