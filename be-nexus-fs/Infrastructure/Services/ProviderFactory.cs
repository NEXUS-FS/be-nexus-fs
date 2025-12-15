using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ProviderFactory
    {
        public ProviderFactory() { }

        public Provider CreateProvider(string providerType, string providerId)
        {
            // Validation logic is centralized in InstantiateProvider
            return InstantiateProvider(providerType, providerId);
        }

        public async Task<Provider> CreateProviderAsync(string providerType, string providerId, Dictionary<string, string> configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            
            var provider = InstantiateProvider(providerType, providerId);
            await provider.Initialize(configuration);
            return provider;
        }

        public Provider CreateProvider(string providerType, string providerId, Dictionary<string, string> configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var provider = InstantiateProvider(providerType, providerId);
            
            // Sync wait is safe here for tests/startup
            provider.Initialize(configuration).GetAwaiter().GetResult(); 
            return provider;
        }

        private Provider InstantiateProvider(string providerType, string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerType))
                throw new ArgumentException("Provider type cannot be empty", nameof(providerType));

            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));

            return providerType.ToLowerInvariant() switch
            {
                "local" or "filesystem" => new LocalProvider(providerId),
           //     "memory" => new MemoryProvider(providerId),
                "s3" or "aws" => new S3Provider(providerId),
                "googledrive" or "gdrive" or "google" or "drive" => new GoogleDriveProvider(providerId),
                "ftp" or "ftps" or "sftp" => new FtpProvider(providerId),
                _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported.")
            };
        }

        public IEnumerable<string> GetSupportedProviderTypes() => new[] { "Local", "Memory", "S3", "GoogleDrive", "FTP", "FTPS", "SFTP" };

        public bool IsProviderTypeSupported(string providerType)
        {
            if (string.IsNullOrWhiteSpace(providerType)) return false;

            var supportedTypes = new[] { "local", "filesystem", "memory", "s3", "aws", "googledrive", "gdrive", "google", "drive", "ftp", "ftps", "sftp" };
            
            return supportedTypes.Contains(providerType.ToLowerInvariant());
        }
    }
}