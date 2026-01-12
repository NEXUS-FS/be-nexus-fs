using Domain.Models;
using Domain.Repositories;
using Infrastructure.Services;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for file operations across storage providers.
    /// Acts as a bridge between the Application layer and Infrastructure providers.
    /// </summary>
    public class FileOperationRepository : IFileOperationRepository
    {
        private readonly ProviderManager _providerManager;

        public FileOperationRepository(ProviderManager providerManager)
        {
            _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
        }

        public async Task<string> ReadFileAsync(string providerId, string filePath)
        {
            var provider = await GetProviderAsync(providerId);
            return await provider.ReadFileAsync(filePath);
        }

        public async Task WriteFileAsync(string providerId, string filePath, string content)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.WriteFileAsync(filePath, content);
        }

        public async Task DeleteFileAsync(string providerId, string filePath)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.DeleteFileAsync(filePath);
        }

        public async Task<List<string>> ListFilesAsync(string providerId, string directoryPath, bool recursive)
        {
            var provider = await GetProviderAsync(providerId);
            return await provider.ListFilesAsync(directoryPath, recursive);
        }

        public async Task<FileMetadata> StatAsync(string providerId, string path)
        {
            var provider = await GetProviderAsync(providerId);
            return await provider.StatAsync(path);
        }

        public async Task MkdirAsync(string providerId, string path, bool recursive)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.MkdirAsync(path, recursive);
        }

        public async Task CopyAsync(string providerId, string sourcePath, string destinationPath)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.CopyAsync(sourcePath, destinationPath);
        }

        public async Task MoveAsync(string providerId, string sourcePath, string destinationPath)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.MoveAsync(sourcePath, destinationPath);
        }

        public async Task<bool> ExistsAsync(string providerId, string path)
        {
            var provider = await GetProviderAsync(providerId);
            return await provider.ExistsAsync(path);
        }

        public async Task<Stream> ReadStreamAsync(string providerId, string filePath)
        {
            var provider = await GetProviderAsync(providerId);
            return await provider.ReadStreamAsync(filePath);
        }

        public async Task WriteStreamAsync(string providerId, string filePath, Stream content)
        {
            var provider = await GetProviderAsync(providerId);
            await provider.WriteStreamAsync(filePath, content);
        }

        public async Task<bool> ProviderExistsAsync(string providerId)
        {
            var provider = await _providerManager.GetProvider(providerId);
            return provider != null;
        }

        private async Task<Provider> GetProviderAsync(string providerId)
        {
            var provider = await _providerManager.GetProvider(providerId);
            if (provider == null)
            {
                throw new KeyNotFoundException($"Provider '{providerId}' not found");
            }
            return provider;
        }
    }
}

