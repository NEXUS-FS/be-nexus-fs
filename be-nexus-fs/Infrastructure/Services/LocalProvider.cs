namespace Infrastructure.Services;

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
    }

    /// <summary>
    /// Reads file content from local file system.
    /// </summary>
    public override async Task<string> ReadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_basePath))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");

        var fullPath = Path.Combine(_basePath, filePath);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(fullPath);
    }

    /// <summary>
    /// Writes content to a file in local file system.
    /// </summary>
    public override async Task WriteFileAsync(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(_basePath))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");

        var fullPath = Path.Combine(_basePath, filePath);
        var directory = Path.GetDirectoryName(fullPath);

        // Create directory if it doesn't exist
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
        if (string.IsNullOrWhiteSpace(_basePath))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");

        var fullPath = Path.Combine(_basePath, filePath);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        await Task.Run(() => File.Delete(fullPath));
    }

    /// <summary>
    /// Tests connection to local file system.
    /// </summary>
    public override async Task<bool> TestConnectionAsync()
    {
        await Task.CompletedTask;
        
        if (string.IsNullOrWhiteSpace(_basePath))
            return false;
            
        return Directory.Exists(_basePath);
    }

	public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
{
    await Task.CompletedTask;
    
    if (string.IsNullOrWhiteSpace(_basePath))
        throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
    
    var fullPath = Path.Combine(_basePath, directoryPath);
    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
    
    if (!Directory.Exists(fullPath))
        return new List<string>();

    return Directory.GetFiles(fullPath, "*", searchOption)
        .Select(f => Path.GetRelativePath(_basePath, f))
        .ToList();
}

}
