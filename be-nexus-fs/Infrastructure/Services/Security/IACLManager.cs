using Application.Common;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Interface for Access Control List management.
    /// Provides permission validation for users accessing resources.
    /// </summary>
    public interface IACLManager
    {
        /// <summary>
        /// Checks if a user has access to perform a specific operation on a resource path.
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="path">The resource path being accessed</param>
        /// <param name="operation">The file operation being performed</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasAccessAsync(string userId, string path, FileOperation operation);
    }
}
