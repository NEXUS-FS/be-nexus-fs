using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class LocalProvider : Provider
    {
        private string _basePath = string.Empty;

        public LocalProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
            : base(providerId, providerType, configuration)
        {
        }

        public LocalProvider(string providerId) 
            : base(providerId, "Local", new Dictionary<string, string>())
        {
        }

        public override async Task Initialize(Dictionary<string, string> config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
            
            if (config.TryGetValue("basePath", out var basePath))
            {
                _basePath = basePath;
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }
            }
            else
            {
                throw new ArgumentException("basePath configuration is required for LocalProvider");
            }
            
            await Task.CompletedTask;
        }

        public override async Task<string> ReadFileAsync(string filePath)
        {
            EnsureInitialized();

          
            var fullPath = GetSecurePath(filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return await File.ReadAllTextAsync(fullPath);
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            EnsureInitialized();

           
            var fullPath = GetSecurePath(filePath);
            
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            EnsureInitialized();

           
            var fullPath = GetSecurePath(filePath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }

        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            EnsureInitialized();

          
            var targetRelative = directoryPath ?? string.Empty;
            var fullSearchPath = GetSecurePath(targetRelative);

            if (!Directory.Exists(fullSearchPath))
                return new List<string>();

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return await Task.Run(() => 
            {
                return Directory.GetFiles(fullSearchPath, "*", searchOption)
                    .Select(f => Path.GetRelativePath(_basePath, f))
                    .Select(p => p.Replace("\\", "/")) 
                    .ToList();
            });
        }

        public override async Task<bool> TestConnectionAsync()
        {
            await Task.CompletedTask;
            if (string.IsNullOrWhiteSpace(_basePath)) return false;
            return Directory.Exists(_basePath);
        }

        /// <summary>
        /// Checks if a file exists on the local file system.
        /// </summary>
        public override async Task<bool> ExistsAsync(string filePath)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("ExistsAsync is not yet implemented for LocalProvider");
        }

        /// <summary>
        /// Gets file metadata/statistics from the local file system.
        /// </summary>
        public override async Task<Dictionary<string, object>> StatAsync(string filePath)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("StatAsync is not yet implemented for LocalProvider");
        }

        private void EnsureInitialized()
        {
            if (string.IsNullOrWhiteSpace(_basePath))
                throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
        }

        /// <summary>
        /// Prevents Path Traversal Attacks (e.g. "../windows/system32")
        /// </summary>
        private string GetSecurePath(string path)
        {
            // 1. Resolve the absolute path of the base directory
            var baseFull = Path.GetFullPath(_basePath);

            // 2. Combine base + user input and Resolve that absolute path
            // Note: We normalize slashes to ensure Path.Combine works consistently
            var normalizedInput = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            var combined = Path.GetFullPath(Path.Combine(_basePath, normalizedInput));

            // 3. check if the result is still inside the base directory
            if (!combined.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access to paths outside the base directory is denied.");
            }

            return combined;
        }
    }
}
