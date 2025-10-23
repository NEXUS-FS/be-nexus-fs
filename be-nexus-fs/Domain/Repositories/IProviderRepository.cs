using Domain.Entities;

/// <summary>
/// Repository interface for Provider entity operations.
/// </summary>

namespace Domain.Repositories
{
    public interface IProviderRepository
    {
        Task<ProviderEntity> GetByIdAsync(string id);
        Task<IEnumerable<ProviderEntity>> GetAllAsync();
        Task<ProviderEntity> AddAsync(ProviderEntity provider);
        Task UpdateAsync(ProviderEntity provider);
        Task DeleteAsync(string id);
        Task<IEnumerable<ProviderEntity>> GetActiveProvidersAsync();
        Task<ProviderEntity> GetByNameAsync(string name);
    }
}
