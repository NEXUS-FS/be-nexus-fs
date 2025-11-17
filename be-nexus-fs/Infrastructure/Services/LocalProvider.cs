namespace Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
/// <summary>
/// Local file system storage provider implementation.
/// </summary>
public class LocalProvider : Provider
{
    private string _basePath = string.Empty;

    public LocalProvider(string providerId, string providerType, Dictionary<string, string> configuration)
        : base(providerId, providerType, configuration)
    {
    }

    // Convenience constructor for ProviderFactory
    public LocalProvider(string providerId)
        : base(providerId, "Local", new Dictionary<string, string>())
    {
    }

    /// <summary>
    /// Initializes the local provider with configuration.
    /// </summary>
    public override async Task Initialize(Dictionary<string, string> config)
    {
        await Task.CompletedTask;

        Configuration = config ?? throw new ArgumentNullException(nameof(config));

        if (config.TryGetValue("basePath", out var basePath))
        {
            _basePath = basePath;

            // Create directory if it doesn't exist
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

    /// <summary>
    /// Reads file content from local file system.
    /// </summary>
    public override async Task<string> ReadFileAsync(string filePath)
    {
        EnsureInitialized();
        var fullPath = GetSecurePath(filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(fullPath);
    }

    /// <summary>
    /// Writes content to a file in local file system.
    /// </summary>
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

    /// <summary>
    /// Deletes a file from local file system.
    /// </summary>
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

    /// <summary>
    /// Tests connection to local file system.
    /// </summary>
    public override async Task<bool> TestConnectionAsync()
    {
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(_basePath)) return false;

        try
        {
            // verify we can actually read the directory info
            return Directory.Exists(_basePath);
        }
        catch
        {
            return false;
        }
    }
    //some helpers methods


    private void EnsureInitialized()
    {
        if (string.IsNullOrWhiteSpace(_basePath))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
    }
    /// <summary>
    /// Combines paths and checks for directory traversal attacks.
    /// </summary>
    private string GetSecurePath(string path)
    {
        var combined = Path.GetFullPath(Path.Combine(_basePath, path));
        var baseFull = Path.GetFullPath(_basePath);

        if (!combined.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access to paths outside the base directory is denied.");
        }

        return combined;
    }
}

