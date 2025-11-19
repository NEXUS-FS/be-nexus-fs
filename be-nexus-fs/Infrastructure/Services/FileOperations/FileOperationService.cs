using Domain.Repositories;
using Infrastructure.Services;
using System.Text.Json;

namespace Infrastructure.Services.FileOperations
{
    /// <summary>
    /// Implementation of file operation repository that manages provider interaction.
    /// </summary>
    public class FileOperationRepository : IFileOperationRepository
    {
        private readonly IProviderRepository _providerRepository;
        private readonly ProviderFactory _providerFactory;

        public FileOperationRepository(
            IProviderRepository providerRepository,
            ProviderFactory providerFactory)
        {
            _providerRepository = providerRepository;
            _providerFactory = providerFactory;
        }

        public async Task<string> ReadFileAsync(string providerId, string filePath)
        {
            var provider = await GetProviderInstanceAsync(providerId);
            if (provider == null)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' not found or inactive");
            }

            return await provider.ReadFileAsync(filePath);
        }

        public async Task WriteFileAsync(string providerId, string filePath, string content)
        {
            var provider = await GetProviderInstanceAsync(providerId);
            if (provider == null)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' not found or inactive");
            }

            await provider.WriteFileAsync(filePath, content);
        }

        public async Task DeleteFileAsync(string providerId, string filePath)
        {
            var provider = await GetProviderInstanceAsync(providerId);
            if (provider == null)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' not found or inactive");
            }

            await provider.DeleteFileAsync(filePath);
        }

        public async Task<List<string>> ListFilesAsync(string providerId, string directoryPath, bool recursive)
        {
            var provider = await GetProviderInstanceAsync(providerId);
            if (provider == null)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' not found or inactive");
            }

            return await provider.ListFilesAsync(directoryPath, recursive);
        }

        public async Task<bool> ProviderExistsAsync(string providerId)
        {
            var providerEntity = await _providerRepository.GetByIdAsync(providerId);
            return providerEntity != null && providerEntity.IsActive;
        }

        private async Task<Provider?> GetProviderInstanceAsync(string providerId)
        {
            var providerEntity = await _providerRepository.GetByIdAsync(providerId);
            if (providerEntity == null || !providerEntity.IsActive)
            {
                return null;
            }

            var configuration = string.IsNullOrWhiteSpace(providerEntity.Configuration)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(providerEntity.Configuration)
                  ?? new Dictionary<string, string>();

            var provider = await _providerFactory.CreateProviderAsync(
                providerEntity.Type,
                providerEntity.Id,
                configuration);

            return provider;
        }
    }
}
