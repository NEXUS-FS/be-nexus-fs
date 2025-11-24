using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface ISandboxPolicyRepository
    {
        /// <summary>
        /// Retrieves the active governance policy for a user.
        /// If no specific policy exists, implementations should return a default policy or null.
        /// </summary>
        Task<SandboxPolicy?> GetPolicyForUserAsync(string userId);
    }
}