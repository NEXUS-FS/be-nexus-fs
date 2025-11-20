using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override async Task<string> ReadFileAsync(string filePath)
        {
            var key = NormalizePath(filePath);

            if (!_storage.TryGetValue(key, out var data))
            {
                throw new System.IO.FileNotFoundException($"File not found in memory: {filePath}");
            }

            return Encoding.UTF8.GetString(data);
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