using Application.DTOs.User;

namespace Application.Services
{
    /// <summary>
    /// Interface for Clerk User Management API (defined in Application layer)
    /// </summary>
    public interface IClerkUserService
    {
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<List<UserDto>> GetUsersAsync(int limit = 10, int offset = 0);
        Task<List<UserDto>> SearchUsersAsync(string query, int limit = 10, int offset = 0);
    }
}