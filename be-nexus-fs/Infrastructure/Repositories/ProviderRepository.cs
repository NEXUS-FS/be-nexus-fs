using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for Provider entity CRUD operations.
    /// </summary>
    public class ProviderRepository : IProviderRepository
    {
        private readonly NexusFSDbContext _context;

        public ProviderRepository(NexusFSDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets a provider by its unique identifier.
        /// </summary>
        public async Task<ProviderEntity> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Gets all providers from the database.
        /// </summary>
        public async Task<IEnumerable<ProviderEntity>> GetAllAsync()
        {
            return await _context.Providers
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new provider to the database.
        /// </summary>
        public async Task<ProviderEntity> AddAsync(ProviderEntity provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Generate ID if not provided
            if (string.IsNullOrWhiteSpace(provider.Id))
            {
                provider.Id = Guid.NewGuid().ToString();
            }

            await _context.Providers.AddAsync(provider);
            await _context.SaveChangesAsync();
            
            return provider;
        }

        /// <summary>
        /// Updates an existing provider in the database.
        /// </summary>
        public async Task UpdateAsync(ProviderEntity provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var existingProvider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == provider.Id);

            if (existingProvider == null)
                throw new KeyNotFoundException($"Provider with ID '{provider.Id}' not found.");

            // Update properties
            existingProvider.Name = provider.Name;
            existingProvider.Type = provider.Type;
            existingProvider.IsActive = provider.IsActive;
            existingProvider.Configuration = provider.Configuration;
            existingProvider.Priority = provider.Priority;
            existingProvider.MaxFileSize = provider.MaxFileSize;
            existingProvider.SupportedFileTypes = provider.SupportedFileTypes;

            _context.Providers.Update(existingProvider);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a provider by its ID (soft delete recommended).
        /// </summary>
      public async Task DeleteAsync(string id)
{
    if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID required", nameof(id));

    // NOTE: We must check if it exists. 
    // If the QueryFilter is active, GetByIdAsync might return null if it was ALREADY deleted.
    // To allow re-deleting (idempotency) or specific checks, fetch normally.
    var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == id);
    
    if (provider != null)
    {
        provider.IsActive = false;
        provider.DeletedAt = DateTime.UtcNow;
        
        _context.Providers.Update(provider);
        await _context.SaveChangesAsync();
    }
    else
    {
         // Optional: Throw exception if you want strict behavior
         throw new KeyNotFoundException($"Provider {id} not found.");
    }
}

        /// <summary>
        /// Gets all active providers (IsActive = true).
        /// </summary>
        public async Task<IEnumerable<ProviderEntity>> GetActiveProvidersAsync()
        {
            return await _context.Providers
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority) // Assuming Priority: lower = higher priority
                .ToListAsync();
        }

        /// <summary>
        /// Gets a provider by its name.
        /// </summary>
        public async Task<ProviderEntity?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _context.Providers
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
        }
    }
}