using Infrastructure.Configuration;
using Application.DTOs.User;
using Application.Services;
using Microsoft.Extensions.Options;
using Clerk.BackendAPI;
using Clerk.BackendAPI.Models.Operations;
using Clerk.BackendAPI.Models.Components;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Service for interacting with Clerk's User Management API using the official BackendAPI SDK
    /// </summary>
    public class ClerkUserService : IClerkUserService
    {
        private readonly ClerkBackendApi _clerkClient;
        private readonly ClerkOptions _clerkOptions;

        public ClerkUserService(IOptions<ClerkOptions> clerkOptions)
        {
            _clerkOptions = clerkOptions.Value;
            
            // Initialize Clerk client with the secret key
            _clerkClient = new ClerkBackendApi(bearerAuth: _clerkOptions.ApiKey);
            
            Console.WriteLine($"Configuring Clerk SDK with API Key: {_clerkOptions.ApiKey}");
        }

        /// <summary>
        /// Get a user by their Clerk user ID
        /// </summary>
        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            try
            {
                var response = await _clerkClient.Users.GetAsync(userId);
                return response?.User != null ? MapToDto(response.User) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user from Clerk: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get a user by their email address
        /// </summary>
        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var request = new GetUserListRequest
                {
                    EmailAddress = new List<string> { email },
                    Limit = 1
                };
                
                var response = await _clerkClient.Users.ListAsync(request);
                // The response has UserList property containing the array of users
                var user = response?.UserList?.FirstOrDefault();
                
                return user != null ? MapToDto(user) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user by email from Clerk: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get a paginated list of users
        /// </summary>
        public async Task<List<UserDto>> GetUsersAsync(int limit = 10, int offset = 0)
        {
            try
            {
                var request = new GetUserListRequest
                {
                    Limit = limit,
                    Offset = offset
                };

                var response = await _clerkClient.Users.ListAsync(request);

                return response?.UserList?.Select(MapToDto).ToList() ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users from Clerk: {ex.Message}");
                return new List<UserDto>();
            }
        }

        /// <summary>
        /// Search users by query (supports email, phone, username, etc.)
        /// </summary>
        public async Task<List<UserDto>> SearchUsersAsync(string query, int limit = 10, int offset = 0)
        {
            try
            {
                var request = new GetUserListRequest
                {
                    Query = query,
                    Limit = limit,
                    Offset = offset
                };

                var response = await _clerkClient.Users.ListAsync(request);

                return response?.UserList?.Select(MapToDto).ToList() ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching users in Clerk: {ex.Message}");
                return new List<UserDto>();
            }
        }

        private UserDto MapToDto(User clerkUser)
        {
            // Get primary email from email addresses
            var primaryEmail = clerkUser.EmailAddresses?.FirstOrDefault()?.EmailAddressValue ?? "";
            
            return new UserDto
            {
                Id = clerkUser.Id ?? "",
                Username = clerkUser.Username ?? primaryEmail ?? clerkUser.Id ?? "",
                Email = primaryEmail,
                Role = "User", // Default role for Clerk users
                Provider = "Clerk",
                IsActive = !clerkUser.Banned && !clerkUser.Locked,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(clerkUser.CreatedAt).DateTime,
                UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(clerkUser.UpdatedAt).DateTime,
                LastLogin = clerkUser.LastSignInAt.HasValue && clerkUser.LastSignInAt.Value > 0 
                    ? DateTimeOffset.FromUnixTimeMilliseconds(clerkUser.LastSignInAt.Value).DateTime 
                    : null
            };
        }
    }
}