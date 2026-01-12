using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebDAVClient;
using Infrastructure.Services.Observability;
using Domain.Models;

namespace Infrastructure.Services
{
    /// <summary>
    /// WebDAV storage provider implementation.
    /// Supports WebDAV protocol for remote file system operations.
    /// </summary>
    public class WebDAVProvider : Provider, IAsyncDisposable
    {
        private string _serverUrl = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _basePath = string.Empty;
        private IClient? _client;
        private readonly Logger? _logger;
        private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);

        public WebDAVProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
            : this(providerId, providerType, configuration, null)
        {
        }

        // Convenience constructor for ProviderFactory
        public WebDAVProvider(string providerId) 
            : this(providerId, null)
        {
        }

        // Internal/testing-friendly constructor with logger injection
        public WebDAVProvider(string providerId, Logger? logger = null)
            : this(providerId, "WebDAV", new Dictionary<string, string>(), logger)
        {
        }

        // Full constructor with logger support
        public WebDAVProvider(string providerId, string providerType, Dictionary<string, string> configuration, Logger? logger) 
            : base(providerId, providerType, configuration)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the WebDAV provider with configuration.
        /// </summary>
        public override async Task Initialize(Dictionary<string, string> config)
        {
            await Task.CompletedTask;

            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            if (!config.TryGetValue("serverUrl", out var serverUrlValue) || string.IsNullOrWhiteSpace(serverUrlValue))
                throw new ArgumentException("serverUrl configuration is required for WebDAVProvider");
            _serverUrl = serverUrlValue!;

            if (!config.TryGetValue("username", out var usernameValue) || string.IsNullOrWhiteSpace(usernameValue))
                throw new ArgumentException("username configuration is required for WebDAVProvider");
            _username = usernameValue!;

            if (!config.TryGetValue("password", out var passwordValue) || string.IsNullOrWhiteSpace(passwordValue))
                throw new ArgumentException("password configuration is required for WebDAVProvider");
            _password = passwordValue!;

            // Base path is optional (default to root)
            if (config.TryGetValue("basePath", out var basePathValue) && !string.IsNullOrWhiteSpace(basePathValue))
            {
                _basePath = basePathValue!;
            }

            // Create and cache the WebDAV client
            await EnsureClientAsync();
        }

        /// <summary>
        /// Creates and configures a WebDAV client.
        /// </summary>
        private IClient CreateWebDAVClient()
        {
            var credentials = new NetworkCredential(_username, _password);
            return new Client(credentials);
        }

        /// <summary>
        /// Ensures the WebDAV client is created.
        /// </summary>
        private async Task<IClient> EnsureClientAsync()
        {
            await _clientLock.WaitAsync();
            try
            {
                _client ??= CreateWebDAVClient();
                return _client;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        /// <summary>
        /// Reads file content from WebDAV server.
        /// </summary>
        public override async Task<string> ReadFileAsync(string filePath)
        {
            ValidateInitialization();

            var fullUrl = GetFullUrl(filePath);
            var client = await EnsureClientAsync();
            
            try
            {
                // Download to memory stream and read as text
                using var stream = await client.Download(fullUrl);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to read file from WebDAV: {filePath}", nameof(WebDAVProvider), ex);
                throw new FileNotFoundException($"File not found or inaccessible: {filePath}", ex);
            }
        }

        /// <summary>
        /// Writes content to a file on WebDAV server.
        /// </summary>
        public override async Task WriteFileAsync(string filePath, string content)
        {
            ValidateInitialization();

            var fullUrl = GetFullUrl(filePath);
            var client = await EnsureClientAsync();
            
            try
            {
                // Ensure parent directories exist
                var directoryPath = GetDirectoryPath(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                {
                    await EnsureDirectoryExistsAsync(directoryPath);
                }
                
                // Upload text content
                var contentBytes = Encoding.UTF8.GetBytes(content);
                using var stream = new MemoryStream(contentBytes);
                var fileName = Path.GetFileName(filePath);
                var parentPath = Path.GetDirectoryName(filePath)?.Replace("\\", "/");
                var parentUrl = parentPath != null ? GetFullUrl(parentPath) : _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
                await client.Upload(parentUrl, stream, fileName);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to write file to WebDAV: {filePath}", nameof(WebDAVProvider), ex);
                throw new IOException($"Failed to write file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Deletes a file from WebDAV server.
        /// </summary>
        public override async Task DeleteFileAsync(string filePath)
        {
            ValidateInitialization();

            var fullUrl = GetFullUrl(filePath);
            var client = await EnsureClientAsync();
            
            try
            {
                await client.DeleteFile(fullUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to delete file from WebDAV: {filePath}", nameof(WebDAVProvider), ex);
                throw new IOException($"Failed to delete file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Tests connection to WebDAV server.
        /// </summary>
        public override async Task<bool> TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                return false;

            try
            {
                var client = await EnsureClientAsync();
                var testUrl = _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
                
                // Try to list root directory as connection test
                var items = await client.List(testUrl);
                return items != null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    $"Connection test failed for provider {ProviderId} (Server: {_serverUrl})",
                    nameof(WebDAVProvider),
                    ex);
                return false;
            }
        }

        /// <summary>
        /// Lists files in a directory on the WebDAV server.
        /// </summary>
        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            ValidateInitialization();
            
            var fullUrl = GetFullUrl(directoryPath);
            var files = new List<string>();
            var client = await EnsureClientAsync();
            
            try
            {
                await ListFilesRecursiveAsync(client, fullUrl, directoryPath, files, recursive);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to list files from WebDAV: {directoryPath}", nameof(WebDAVProvider), ex);
                // Return empty list instead of throwing
                return new List<string>();
            }
            
            return files;
        }

        /// <summary>
        /// Recursively lists files from WebDAV server.
        /// </summary>
        private async Task ListFilesRecursiveAsync(IClient client, string fullUrl, string relativePath, List<string> files, bool recursive)
        {
            var items = await client.List(fullUrl);
            
            foreach (var item in items)
            {
                // Skip current and parent directory entries
                if (item.Href == "." || item.Href == ".." || string.IsNullOrWhiteSpace(item.Href))
                    continue;

                var itemRelativePath = string.IsNullOrWhiteSpace(relativePath) 
                    ? item.Href.TrimStart('/') 
                    : Path.Combine(relativePath, item.Href).Replace("\\", "/");

                if (item.IsCollection)
                {
                    if (recursive)
                    {
                        var itemFullUrl = fullUrl.TrimEnd('/') + "/" + item.Href.TrimStart('/');
                        await ListFilesRecursiveAsync(client, itemFullUrl, itemRelativePath, files, recursive);
                    }
                }
                else
                {
                    files.Add(itemRelativePath);
                }
            }
        }

        /// <summary>
        /// Ensures a directory exists on the WebDAV server.
        /// </summary>
        private async Task EnsureDirectoryExistsAsync(string directoryPath)
        {
            var client = await EnsureClientAsync();
            var pathParts = directoryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var currentPath = _basePath.TrimStart('/');

            foreach (var part in pathParts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                var fullUrl = _serverUrl.TrimEnd('/') + "/" + currentPath;
                
                try
                {
                    // Try to create directory (will fail silently if already exists)
                    var parentUrl = currentPath.Contains('/') 
                        ? _serverUrl.TrimEnd('/') + "/" + currentPath.Substring(0, currentPath.LastIndexOf('/'))
                        : _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
                    await client.CreateDir(parentUrl, part);
                }
                catch
                {
                    // Directory might already exist, which is fine
                    // WebDAV doesn't have a standard "check if exists" operation
                }
            }
        }

        public override async Task<FileMetadata> StatAsync(string path)
        {
            ValidateInitialization();
            var fullUrl = GetFullUrl(path);
            var client = await EnsureClientAsync();

            var metadata = new FileMetadata
            {
                Path = path,
                Name = Path.GetFileName(path) ?? path,
                Exists = false
            };

            try
            {
                var item = await client.GetFolder(fullUrl);
                if (item != null)
                {
                    metadata.Exists = true;
                    metadata.IsDirectory = item.IsCollection;
                    metadata.Size = item.ContentLength ?? 0;
                    metadata.Modified = item.LastModified;
                    metadata.ContentType = item.ContentType;
                }
            }
            catch
            {
                metadata.Exists = false;
            }

            return metadata;
        }

        public override async Task MkdirAsync(string path, bool recursive = true)
        {
            ValidateInitialization();
            var fullUrl = GetFullUrl(path);
            var client = await EnsureClientAsync();

            if (recursive)
            {
                // Ensure all parent directories exist
                await EnsureDirectoryExistsAsync(path);
            }
            else
            {
                // Just create the final directory
                var parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
                var dirName = Path.GetFileName(path);
                var parentUrl = parentPath != null ? GetFullUrl(parentPath) : _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
                await client.CreateDir(parentUrl, dirName);
            }
        }

        public override async Task CopyAsync(string sourcePath, string destinationPath)
        {
            ValidateInitialization();
            
            // WebDAV doesn't have native copy (usually), so download and re-upload
            var sourceUrl = GetFullUrl(sourcePath);
            var destUrl = GetFullUrl(destinationPath);
            var client = await EnsureClientAsync();

            // Ensure destination directory exists
            var destDir = GetDirectoryPath(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && destDir != "/")
            {
                await EnsureDirectoryExistsAsync(destDir);
            }

            using var stream = await client.Download(sourceUrl);
            stream.Position = 0;
            var destFileName = Path.GetFileName(destinationPath);
            var destParentPath = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/");
            var destParentUrl = destParentPath != null ? GetFullUrl(destParentPath) : _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
            await client.Upload(destParentUrl, stream, destFileName);
        }

        public override async Task MoveAsync(string sourcePath, string destinationPath)
        {
            ValidateInitialization();
            var sourceUrl = GetFullUrl(sourcePath);
            var destUrl = GetFullUrl(destinationPath);
            var client = await EnsureClientAsync();

            // Ensure destination directory exists
            var destDir = GetDirectoryPath(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && destDir != "/")
            {
                await EnsureDirectoryExistsAsync(destDir);
            }

            await client.MoveFile(sourceUrl, destUrl);
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            ValidateInitialization();
            var fullUrl = GetFullUrl(path);
            var client = await EnsureClientAsync();

            try
            {
                var item = await client.GetFolder(fullUrl);
                return item != null;
            }
            catch
            {
                return false;
            }
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            ValidateInitialization();
            var fullUrl = GetFullUrl(filePath);
            var client = await EnsureClientAsync();
            
            try
            {
                return await client.Download(fullUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to read file stream from WebDAV: {filePath}", nameof(WebDAVProvider), ex);
                throw new FileNotFoundException($"File not found or inaccessible: {filePath}", ex);
            }
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            ValidateInitialization();
            var fullUrl = GetFullUrl(filePath);
            var client = await EnsureClientAsync();
            
            try
            {
                // Ensure parent directories exist
                var directoryPath = GetDirectoryPath(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                {
                    await EnsureDirectoryExistsAsync(directoryPath);
                }
                
                var fileName = Path.GetFileName(filePath);
                var parentPath = Path.GetDirectoryName(filePath)?.Replace("\\", "/");
                var parentUrl = parentPath != null ? GetFullUrl(parentPath) : _serverUrl.TrimEnd('/') + "/" + _basePath.TrimStart('/');
                await client.Upload(parentUrl, content, fileName);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to write file stream to WebDAV: {filePath}", nameof(WebDAVProvider), ex);
                throw new IOException($"Failed to write file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Disposes the cached WebDAV client.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _clientLock.WaitAsync();
            try
            {
                _client = null;
            }
            finally
            {
                _clientLock.Release();
                _clientLock.Dispose();
            }
        }

        #region Private Helper Methods

        private void ValidateInitialization()
        {
            if (string.IsNullOrWhiteSpace(_serverUrl))
                throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
        }

        /// <summary>
        /// Constructs full WebDAV URL from relative path.
        /// </summary>
        private string GetFullUrl(string relativePath)
        {
            var normalizedPath = NormalizePath(relativePath);
            var basePath = _basePath.TrimStart('/').TrimEnd('/');
            
            var fullPath = string.IsNullOrEmpty(basePath) 
                ? normalizedPath 
                : $"{basePath}/{normalizedPath}";
            
            return $"{_serverUrl.TrimEnd('/')}/{fullPath.TrimStart('/')}";
        }

        /// <summary>
        /// Normalizes a file or directory path for WebDAV operations.
        /// </summary>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == ".")
            {
                return "";
            }
            
            // Replace backslashes with forward slashes and remove leading/trailing slashes
            return path.Replace("\\", "/").Trim('/');
        }

        /// <summary>
        /// Extracts the directory path from a file path.
        /// </summary>
        private string GetDirectoryPath(string filePath)
        {
            var normalized = NormalizePath(filePath);
            var lastSlash = normalized.LastIndexOf('/');
            
            if (lastSlash <= 0)
                return "";
            
            return normalized.Substring(0, lastSlash);
        }

        #endregion
    }
}

