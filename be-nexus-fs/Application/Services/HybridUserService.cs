using Application.DTOs.User;
using Application.DTOs.Common;
using Domain.Entities;
using Domain.Repositories;

namespace Application.Services
{
    /// <summary>
    /// Hybrid user service that can fetch users from both Clerk and the local database
    /// </summary>
    public interface IHybridUserService
    {
        Task<UserDto?> GetUserByIdAsync(string userId, bool preferClerk = false);
        Task<UserDto?> GetUserByEmailAsync(string email, bool preferClerk = false);
        Task<PagedResponse<UserDto>> GetAllUsersAsync(bool includeClerkUsers = false, int pageNumber = 1, int pageSize = 10);
        Task<UserDto?> SyncUserFromClerkAsync(string clerkUserId);
        Task<UserDto?> GetOrCreateUserFromClerkAsync(string clerkUserId);
    }

    public class HybridUserService : IHybridUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IClerkUserService _clerkUserService;

        public HybridUserService(IUserRepository userRepository, IClerkUserService clerkUserService)
        {
            _userRepository = userRepository;
            _clerkUserService = clerkUserService;
        }

        /// <summary>
        /// Get user by ID, optionally preferring Clerk over database
        /// </summary>
        public async Task<UserDto?> GetUserByIdAsync(string userId, bool preferClerk = false)
        {
            if (preferClerk || userId.StartsWith("user_")) // Clerk user IDs start with "user_"
            {
                // Try Clerk first
                var clerkUser = await _clerkUserService.GetUserByIdAsync(userId);
                if (clerkUser != null)
                {
                    return clerkUser;
                }
            }

            // Try database
            var dbUser = await _userRepository.GetByIdAsync(userId);
            if (dbUser != null)
            {
                return MapUserEntityToDto(dbUser);
            }

            // If not found in database and not tried Clerk yet, try Clerk
            if (!preferClerk && !userId.StartsWith("user_"))
            {
                var clerkUser = await _clerkUserService.GetUserByIdAsync(userId);
                if (clerkUser != null)
                {
                    return clerkUser;
                }
            }

            return null;
        }

        /// <summary>
        /// Get user by email, optionally preferring Clerk over database
        /// </summary>
        public async Task<UserDto?> GetUserByEmailAsync(string email, bool preferClerk = false)
        {
            if (preferClerk)
            {
                // Try Clerk first
                var clerkUser = await _clerkUserService.GetUserByEmailAsync(email);
                if (clerkUser != null)
                {
                    return clerkUser;
                }
            }

            // Try database
            var dbUser = await _userRepository.GetByEmailAsync(email);
            if (dbUser != null)
            {
                return MapUserEntityToDto(dbUser);
            }

            // If not found in database and not tried Clerk yet, try Clerk
            if (!preferClerk)
            {
                var clerkUser = await _clerkUserService.GetUserByEmailAsync(email);
                if (clerkUser != null)
                {
                    return clerkUser;
                }
            }

            return null;
        }

        /// <summary>
        /// Get all users, optionally including users from Clerk
        /// </summary>
        public async Task<PagedResponse<UserDto>> GetAllUsersAsync(bool includeClerkUsers = false, int pageNumber = 1, int pageSize = 10)
        {
            var result = new List<UserDto>();

            // Get users from database
            var dbUsers = await _userRepository.GetAllAsync(pageNumber, pageSize);
            result.AddRange(dbUsers.Select(MapUserEntityToDto));

            var clerkUsers = new List<UserDto>(); // Initialize with empty list

            if (includeClerkUsers)
            {
                // Get users from Clerk (excluding those already in database)
                clerkUsers = await _clerkUserService.GetUsersAsync(pageSize, (pageNumber - 1) * pageSize);
                var dbUserClerkIds = dbUsers.Where(u => u.Provider == "Clerk").Select(u => u.ProviderId).ToHashSet();

                foreach (var clerkUser in clerkUsers)
                {
                    if (!dbUserClerkIds.Contains(clerkUser.Id))
                    {
                        result.Add(clerkUser);
                    }
                }
            }

            // Calculate total count
            var totalDbUsers = await _userRepository.GetTotalCountAsync();
            var totalClerkUsers = includeClerkUsers ? clerkUsers.Count : 0;
            var totalCount = totalDbUsers + Math.Max(0, totalClerkUsers - dbUsers.Count(u => u.Provider == "Clerk"));

            return new PagedResponse<UserDto>
            {
                Data = result,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Sync a user from Clerk to the local database
        /// </summary>
        public async Task<UserDto?> SyncUserFromClerkAsync(string clerkUserId)
        {
            var clerkUser = await _clerkUserService.GetUserByIdAsync(clerkUserId);
            if (clerkUser == null)
                return null;

            // Check if user already exists in database
            var existingUser = await _userRepository.GetByProviderIdAsync("Clerk", clerkUserId);

            if (existingUser != null)
            {
                // Update existing user
                existingUser.Email = clerkUser.Email ?? existingUser.Email;
                existingUser.Username = clerkUser.Username ?? clerkUser.Email ?? existingUser.Username;
                existingUser.UpdatedAt = DateTime.UtcNow;
                existingUser.LastLogin = clerkUser.LastLogin ?? existingUser.LastLogin;

                await _userRepository.UpdateAsync(existingUser);
                return MapUserEntityToDto(existingUser);
            }
            else
            {
                // Create new user
                var newUser = new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = clerkUser.Username ?? clerkUser.Email ?? clerkUser.Id,
                    Email = clerkUser.Email ?? $"{clerkUser.Id}@clerk.local",
                    Provider = "Clerk",
                    ProviderId = clerkUser.Id,
                    Role = "User", // Default role
                    IsActive = clerkUser.IsActive,
                    CreatedAt = clerkUser.CreatedAt,
                    UpdatedAt = clerkUser.UpdatedAt ?? DateTime.UtcNow,
                    LastLogin = clerkUser.LastLogin
                };

                await _userRepository.AddAsync(newUser);
                return MapUserEntityToDto(newUser);
            }
        }

        /// <summary>
        /// Get user from database or create from Clerk if not exists
        /// </summary>
        public async Task<UserDto?> GetOrCreateUserFromClerkAsync(string clerkUserId)
        {
            // First try to get from database
            var existingUser = await _userRepository.GetByProviderIdAsync("Clerk", clerkUserId);
            if (existingUser != null)
            {
                return MapUserEntityToDto(existingUser);
            }

            // If not in database, sync from Clerk
            return await SyncUserFromClerkAsync(clerkUserId);
        }

        private UserDto MapUserEntityToDto(UserEntity user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Provider = user.Provider,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLogin
            };
        }

    }
}