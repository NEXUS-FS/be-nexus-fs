using Domain.Entities;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        // Basic CRUD
        Task<UserEntity?> GetByIdAsync(string id);
        Task<IEnumerable<UserEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<UserEntity> AddAsync(UserEntity user);
        Task UpdateAsync(UserEntity user);
        Task DeleteAsync(string id);
        Task RestoreAsync(string id);

        // Authentication queries
        Task<UserEntity?> GetByUsernameAsync(string username);
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByProviderIdAsync(string provider, string providerId);
        Task UpdateLastLoginAsync(string userId);  // ADD THIS LINE
        
        // Refresh token management
        Task UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime expiryTime);
        Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken);
        Task<UserEntity?> ValidateCredentialsAsync(string username, string password);


        // Query helpers
        Task<int> GetTotalCountAsync(bool includeInactive = false);
        Task<bool> UsernameExistsAsync(string username, string? excludeUserId = null);
        Task<bool> EmailExistsAsync(string email, string? excludeUserId = null);
        Task<IEnumerable<UserEntity>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<UserEntity>> GetByRoleAsync(string role, int pageNumber = 1, int pageSize = 10);
    }
}