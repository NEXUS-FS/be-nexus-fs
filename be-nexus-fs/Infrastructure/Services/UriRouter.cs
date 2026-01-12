using Infrastructure.Services.Observability;

namespace Infrastructure.Services
{
    /// <summary>
    /// Routes file operations based on URI schemes (s3://, ftp://, file://, etc.)
    /// </summary>
    public class UriRouter
    {
        private readonly ProviderManager _providerManager;
        private readonly Logger? _logger;

        public UriRouter(ProviderManager providerManager, Logger? logger = null)
        {
            _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
            _logger = logger;
        }

        /// <summary>
        /// Parses a URI and returns the provider ID and file path.
        /// Supported formats:
        /// - s3://bucket-name/path/to/file
        /// - ftp://server/path/to/file
        /// - ftps://server/path/to/file
        /// - webdav://server/path/to/file
        /// - gdrive://folder-id/file-name
        /// - file:///local/path/to/file
        /// - local:///local/path/to/file
        /// </summary>
        public (string providerId, string filePath) ParseUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("URI cannot be empty", nameof(uri));

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
                throw new ArgumentException($"Invalid URI format: {uri}", nameof(uri));

            var scheme = parsedUri.Scheme.ToLowerInvariant();
            var host = parsedUri.Host;
            var path = parsedUri.AbsolutePath.TrimStart('/');

            _logger?.LogInformation($"Parsing URI - Scheme: {scheme}, Host: {host}, Path: {path}", "UriRouter");

            return scheme switch
            {
                "s3" => (FindProviderByTypeAndHost("S3", host), path),
                "ftp" => (FindProviderByTypeAndHost("FTP", host), path),
                "ftps" => (FindProviderByTypeAndHost("FTPS", host), path),
                "webdav" => (FindProviderByTypeAndHost("WebDAV", host), path),
                "gdrive" or "googledrive" => (FindProviderByType("GoogleDrive"), path),
                "file" or "local" => (FindProviderByType("Local"), parsedUri.LocalPath),
                _ => throw new NotSupportedException($"URI scheme '{scheme}' is not supported")
            };
        }

        /// <summary>
        /// Reads a file using URI-based routing.
        /// </summary>
        public async Task<string> ReadFileAsync(string uri)
        {
            var (providerId, filePath) = ParseUri(uri);
            var provider = await _providerManager.GetProvider(providerId);
            
            if (provider == null)
                throw new KeyNotFoundException($"Provider '{providerId}' not found");

            return await provider.ReadFileAsync(filePath);
        }

        /// <summary>
        /// Writes a file using URI-based routing.
        /// </summary>
        public async Task WriteFileAsync(string uri, string content)
        {
            var (providerId, filePath) = ParseUri(uri);
            var provider = await _providerManager.GetProvider(providerId);
            
            if (provider == null)
                throw new KeyNotFoundException($"Provider '{providerId}' not found");

            await provider.WriteFileAsync(filePath, content);
        }

        /// <summary>
        /// Deletes a file using URI-based routing.
        /// </summary>
        public async Task DeleteFileAsync(string uri)
        {
            var (providerId, filePath) = ParseUri(uri);
            var provider = await _providerManager.GetProvider(providerId);
            
            if (provider == null)
                throw new KeyNotFoundException($"Provider '{providerId}' not found");

            await provider.DeleteFileAsync(filePath);
        }

        /// <summary>
        /// Lists files using URI-based routing.
        /// </summary>
        public async Task<List<string>> ListFilesAsync(string uri, bool recursive = false)
        {
            var (providerId, directoryPath) = ParseUri(uri);
            var provider = await _providerManager.GetProvider(providerId);
            
            if (provider == null)
                throw new KeyNotFoundException($"Provider '{providerId}' not found");

            return await provider.ListFilesAsync(directoryPath, recursive);
        }

        /// <summary>
        /// Checks if a file exists using URI-based routing.
        /// </summary>
        public async Task<bool> ExistsAsync(string uri)
        {
            var (providerId, filePath) = ParseUri(uri);
            var provider = await _providerManager.GetProvider(providerId);
            
            if (provider == null)
                throw new KeyNotFoundException($"Provider '{providerId}' not found");

            return await provider.ExistsAsync(filePath);
        }

        /// <summary>
        /// Copies a file from source URI to destination URI.
        /// Supports cross-provider copy operations.
        /// </summary>
        public async Task CopyAsync(string sourceUri, string destinationUri)
        {
            var (sourceProviderId, sourcePath) = ParseUri(sourceUri);
            var (destProviderId, destPath) = ParseUri(destinationUri);

            if (sourceProviderId == destProviderId)
            {
                // Same provider - use native copy
                var provider = await _providerManager.GetProvider(sourceProviderId);
                if (provider == null)
                    throw new KeyNotFoundException($"Provider '{sourceProviderId}' not found");

                await provider.CopyAsync(sourcePath, destPath);
            }
            else
            {
                // Cross-provider copy - read from source and write to destination
                _logger?.LogInformation($"Cross-provider copy from {sourceProviderId} to {destProviderId}", "UriRouter");
                
                var sourceProvider = await _providerManager.GetProvider(sourceProviderId);
                var destProvider = await _providerManager.GetProvider(destProviderId);

                if (sourceProvider == null)
                    throw new KeyNotFoundException($"Source provider '{sourceProviderId}' not found");
                if (destProvider == null)
                    throw new KeyNotFoundException($"Destination provider '{destProviderId}' not found");

                using var stream = await sourceProvider.ReadStreamAsync(sourcePath);
                await destProvider.WriteStreamAsync(destPath, stream);
            }
        }

        /// <summary>
        /// Moves a file from source URI to destination URI.
        /// Supports cross-provider move operations.
        /// </summary>
        public async Task MoveAsync(string sourceUri, string destinationUri)
        {
            var (sourceProviderId, sourcePath) = ParseUri(sourceUri);
            var (destProviderId, destPath) = ParseUri(destinationUri);

            if (sourceProviderId == destProviderId)
            {
                // Same provider - use native move
                var provider = await _providerManager.GetProvider(sourceProviderId);
                if (provider == null)
                    throw new KeyNotFoundException($"Provider '{sourceProviderId}' not found");

                await provider.MoveAsync(sourcePath, destPath);
            }
            else
            {
                // Cross-provider move - copy then delete
                await CopyAsync(sourceUri, destinationUri);
                await DeleteFileAsync(sourceUri);
            }
        }

        private string FindProviderByType(string providerType)
        {
            var providers = _providerManager.GetAllProviders().Result;
            var provider = providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new KeyNotFoundException($"No provider found for type '{providerType}'");

            return provider.ProviderId;
        }

        private string FindProviderByTypeAndHost(string providerType, string host)
        {
            var providers = _providerManager.GetAllProviders().Result;
            
            // Try to find provider by type and matching host in configuration
            var provider = providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase) &&
                (p.Configuration.ContainsKey("host") && p.Configuration["host"].Contains(host, StringComparison.OrdinalIgnoreCase) ||
                 p.Configuration.ContainsKey("bucket") && p.Configuration["bucket"].Equals(host, StringComparison.OrdinalIgnoreCase) ||
                 p.Configuration.ContainsKey("serverUrl") && p.Configuration["serverUrl"].Contains(host, StringComparison.OrdinalIgnoreCase)));

            if (provider != null)
                return provider.ProviderId;

            // Fallback: find first provider of matching type
            provider = providers.FirstOrDefault(p => 
                p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new KeyNotFoundException($"No provider found for type '{providerType}' and host '{host}'");

            _logger?.LogWarning($"Using fallback provider {provider.ProviderId} for {providerType}://{host}", "UriRouter");
            return provider.ProviderId;
        }
    }
}

