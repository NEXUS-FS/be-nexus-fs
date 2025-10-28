using Application.DTOs;
using Domain.Repositories;
using Infrastructure.Services.Security;

/// <summary>
/// Exposes access control and user permission data to the management UI.
/// </summary>

namespace Infrastructure.Services.UI
{
    public class ACLUIService
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IUserRepository _userRepository;
        private readonly SandboxGuard _sandboxGuard;
        private readonly ACLManager _aclManager; //this is in sandbox already.. it may be a good idea to have it here also or to make this a singleton?? 

        public ACLUIService(
            IProviderRepository providerRepository,
            IUserRepository userRepository,
            SandboxGuard sandboxGuard,
            ACLManager aclManager)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _sandboxGuard = sandboxGuard ?? throw new ArgumentNullException(nameof(sandboxGuard));
            _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
        }


        /// Retrieves all users with their assigned permissions.
        public async Task<IEnumerable<UserPermissionsDto>> GetUsersWithPermissionsAsync()
        {
            // TODO: implement mapping from users and ACLManager
            throw new NotImplementedException();
        }


        /// Checks if a user has access to a provider.
        public async Task<bool> UserHasAccessToProviderAsync(string username, int providerId, string permission)
        {
            // TODO: implement actual provider lookup and sandbox/ACL check
            throw new NotImplementedException();
        }


        /// Grants a permission to a user.
        public async Task GrantPermissionAsync(string username, string permission)
        {
            // TODO: implement grant logic
            throw new NotImplementedException();
        }


        /// Revokes a permission from a user.
        public async Task RevokePermissionAsync(string username, string permission)
        {
            // TODO: implement revoke logic
            throw new NotImplementedException();
        }


        /// Retrieves all allowed sandbox paths for a given strategy/user.
        public async Task<IEnumerable<string>> GetSandboxPathsAsync()
        {
            throw new NotImplementedException();
        }


        /// Adds a path to the sandbox’s allowed paths.
        public async Task AddSandboxPathAsync(string path)
        {
            throw new NotImplementedException();
        }

        /// Removes a path from the sandbox’s allowed paths.
        public async Task RemoveSandboxPathAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}
