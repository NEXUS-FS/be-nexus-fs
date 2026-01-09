using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;

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

        public override async Task<FileMetadata> StatAsync(string path)
        {
            EnsureInitialized();
            var fullPath = GetSecurePath(path);

            var metadata = new FileMetadata
            {
                Path = path,
                Name = Path.GetFileName(fullPath),
                Exists = File.Exists(fullPath) || Directory.Exists(fullPath)
            };

            if (!metadata.Exists)
            {
                return metadata;
            }

            if (Directory.Exists(fullPath))
            {
                var dirInfo = new DirectoryInfo(fullPath);
                metadata.IsDirectory = true;
                metadata.Created = dirInfo.CreationTimeUtc;
                metadata.Modified = dirInfo.LastWriteTimeUtc;
                metadata.Size = 0;
            }
            else if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                metadata.IsDirectory = false;
                metadata.Created = fileInfo.CreationTimeUtc;
                metadata.Modified = fileInfo.LastWriteTimeUtc;
                metadata.Size = fileInfo.Length;
                metadata.ContentType = GetContentType(fileInfo.Extension);
            }

            return await Task.FromResult(metadata);
        }

        public override async Task MkdirAsync(string path, bool recursive = true)
        {
            EnsureInitialized();
            var fullPath = GetSecurePath(path);

            if (Directory.Exists(fullPath))
            {
                return; // Already exists
            }

            if (recursive)
            {
                Directory.CreateDirectory(fullPath);
            }
            else
            {
                var parentDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    throw new DirectoryNotFoundException($"Parent directory does not exist: {parentDir}");
                }
                Directory.CreateDirectory(fullPath);
            }

            await Task.CompletedTask;
        }

        public override async Task CopyAsync(string sourcePath, string destinationPath)
        {
            EnsureInitialized();
            var fullSourcePath = GetSecurePath(sourcePath);
            var fullDestPath = GetSecurePath(destinationPath);

            if (!File.Exists(fullSourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }

            var destDirectory = Path.GetDirectoryName(fullDestPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            await Task.Run(() => File.Copy(fullSourcePath, fullDestPath, overwrite: true));
        }

        public override async Task MoveAsync(string sourcePath, string destinationPath)
        {
            EnsureInitialized();
            var fullSourcePath = GetSecurePath(sourcePath);
            var fullDestPath = GetSecurePath(destinationPath);

            if (!File.Exists(fullSourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }

            var destDirectory = Path.GetDirectoryName(fullDestPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            await Task.Run(() => File.Move(fullSourcePath, fullDestPath, overwrite: true));
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            EnsureInitialized();
            var fullPath = GetSecurePath(path);
            return await Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            EnsureInitialized();
            var fullPath = GetSecurePath(filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {filePath}");

            // Return a FileStream that will be disposed by the caller
            return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true));
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            EnsureInitialized();
            var fullPath = GetSecurePath(filePath);
            
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await content.CopyToAsync(fileStream);
        }

        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
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
