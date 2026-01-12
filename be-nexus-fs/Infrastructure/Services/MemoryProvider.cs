using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;

namespace Infrastructure.Services
{
    /// <summary>
    /// In-memory storage provider implementation.
    /// Uses a ConcurrentDictionary to simulate a file system.
    /// </summary>
    public class MemoryProvider : Provider
    {
        // Storage: Key = Normalized File Path, Value = File Content (Bytes)
        private readonly ConcurrentDictionary<string, byte[]> _storage 
            = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public MemoryProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
            : base(providerId, providerType, configuration)
        {
        }

        // Convenience constructor for ProviderFactory
        public MemoryProvider(string providerId) 
            : base(providerId, "Memory", new Dictionary<string, string>())
        {
        }

        public override async Task Initialize(Dictionary<string, string> config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
            
            await Task.CompletedTask;
        }

        public override Task<string> ReadFileAsync(string filePath)
        {
            var key = NormalizePath(filePath);

            if (!_storage.TryGetValue(key, out var data))
            {
                throw new System.IO.FileNotFoundException($"File not found in memory: {filePath}");
            }

            return Task.FromResult(Encoding.UTF8.GetString(data));
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            var key = NormalizePath(filePath);
            var data = Encoding.UTF8.GetBytes(content);

            // Add or Update (Last write wins)
            _storage.AddOrUpdate(key, data, (k, oldValue) => data);
            
            await Task.CompletedTask;
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            var key = NormalizePath(filePath);
            
            // TryRemove returns false if key doesn't exist, which mimics 
            // idempotent delete or we can throw if strict behavior is needed.
            // Standard IO usually throws if file doesn't exist, but for providers
            // silence is often preferred. We will match LocalProvider strictness:
            
            if (!_storage.ContainsKey(key))
            {
                 throw new System.IO.FileNotFoundException($"File not found: {filePath}");
            }

            _storage.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            await Task.CompletedTask;
            
           
            var prefix = NormalizePath(directoryPath);
            
            // If not empty and doesn't end with slash, add one to prevent partial matches 
            // (e.g. "test" matching "testing/file.txt")
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            var query = _storage.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (!recursive)
            {
            
                query = query.Where(k => 
                {
                    var relative = k.Substring(prefix.Length);
                    return !relative.Contains("/");
                });
            }

            return query.OrderBy(k => k).ToList();
        }

        public override async Task<bool> TestConnectionAsync()
        {
            // In-memory is always connected
            return await Task.FromResult(true);
        }

        public override async Task<FileMetadata> StatAsync(string path)
        {
            var key = NormalizePath(path);
            var exists = _storage.ContainsKey(key);

            var metadata = new FileMetadata
            {
                Path = path,
                Name = Path.GetFileName(path) ?? path,
                Exists = exists,
                IsDirectory = false
            };

            if (exists && _storage.TryGetValue(key, out var data))
            {
                metadata.Size = data.Length;
                metadata.ContentType = "application/octet-stream";
                metadata.Created = DateTime.UtcNow; // In-memory doesn't track creation time
                metadata.Modified = DateTime.UtcNow;
            }

            return await Task.FromResult(metadata);
        }

        public override async Task MkdirAsync(string path, bool recursive = true)
        {
            // In-memory provider doesn't have actual directories
            // This is a no-op for compatibility
            await Task.CompletedTask;
        }

        public override async Task CopyAsync(string sourcePath, string destinationPath)
        {
            var sourceKey = NormalizePath(sourcePath);
            var destKey = NormalizePath(destinationPath);

            if (!_storage.TryGetValue(sourceKey, out var data))
            {
                throw new System.IO.FileNotFoundException($"Source file not found: {sourcePath}");
            }

            // Create a copy of the data
            var dataCopy = new byte[data.Length];
            Array.Copy(data, dataCopy, data.Length);
            
            _storage.AddOrUpdate(destKey, dataCopy, (k, oldValue) => dataCopy);
            await Task.CompletedTask;
        }

        public override async Task MoveAsync(string sourcePath, string destinationPath)
        {
            var sourceKey = NormalizePath(sourcePath);
            var destKey = NormalizePath(destinationPath);

            if (!_storage.TryGetValue(sourceKey, out var data))
            {
                throw new System.IO.FileNotFoundException($"Source file not found: {sourcePath}");
            }

            _storage.AddOrUpdate(destKey, data, (k, oldValue) => data);
            _storage.TryRemove(sourceKey, out _);
            
            await Task.CompletedTask;
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            var key = NormalizePath(path);
            return await Task.FromResult(_storage.ContainsKey(key));
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            var key = NormalizePath(filePath);

            if (!_storage.TryGetValue(key, out var data))
            {
                throw new System.IO.FileNotFoundException($"File not found in memory: {filePath}");
            }

            // Return a MemoryStream with the data
            return await Task.FromResult(new MemoryStream(data));
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            var key = NormalizePath(filePath);
            
            // Read stream into memory
            using var memStream = new MemoryStream();
            await content.CopyToAsync(memStream);
            var data = memStream.ToArray();

            _storage.AddOrUpdate(key, data, (k, oldValue) => data);
        }

        /// <summary>
        /// Normalizes paths to use forward slashes for consistent dictionary keys.
        /// </summary>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            // replace backslashes with forward slashes and trim
            return path.Replace("\\", "/").Trim('/');
        }
    }
}