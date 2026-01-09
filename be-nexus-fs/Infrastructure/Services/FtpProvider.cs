using FluentFTP;
using Infrastructure.Services.Observability;
using Domain.Models;

namespace Infrastructure.Services;

/// <summary>
/// FTP storage provider implementation using FluentFTP.
/// </summary>
public class FtpProvider : Provider
{
    private string _host = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _port = 21;
    private AsyncFtpClient? _client;
    private readonly SemaphoreSlim _clientLock = new(1, 1);

    public FtpProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
        : base(providerId, providerType, configuration)
    {
    }

    // Convenience constructor for ProviderFactory
    public FtpProvider(string providerId) 
        : base(providerId, "FTP", new Dictionary<string, string>())
    {
    }

    public override ProviderCapabilities GetCapabilities()
    {
        return ProviderCapabilities.ForFtp();
    }

    /// <summary>
    /// Initializes the FTP provider with configuration.
    /// </summary>
    public override async Task Initialize(Dictionary<string, string> config)
    {
        await Task.CompletedTask;

        Configuration = config ?? throw new ArgumentNullException(nameof(config));

        if (!config.TryGetValue("host", out _host))
            throw new ArgumentException("host configuration is required for FtpProvider");

        if (!config.TryGetValue("username", out _username))
            throw new ArgumentException("username configuration is required for FtpProvider");

        if (!config.TryGetValue("password", out _password))
            throw new ArgumentException("password configuration is required for FtpProvider");

        if (config.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var port))
        {
            _port = port;
        }
    }

    /// <summary>
    /// Reads file content from FTP server.
    /// </summary>
    public override async Task<string> ReadFileAsync(string filePath)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(filePath);
        var client = await EnsureClientAsync();
        
        using var stream = new MemoryStream();
        await client.DownloadStream(stream, normalizedPath);
        stream.Position = 0;
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Writes content to a file on FTP server.
    /// </summary>
    public override async Task WriteFileAsync(string filePath, string content)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(filePath);
        var client = await EnsureClientAsync();
        
        // Ensure destination directory exists
        var destDir = GetDirectoryPath(normalizedPath);
        if (!string.IsNullOrEmpty(destDir) && destDir != "/")
        {
            await client.CreateDirectory(destDir, force: true);
        }
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await client.UploadStream(stream, normalizedPath, createRemoteDir: true);
    }

    /// <summary>
    /// Deletes a file from FTP server.
    /// </summary>
    public override async Task DeleteFileAsync(string filePath)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(filePath);
        var client = await EnsureClientAsync();
        
        await client.DeleteFile(normalizedPath);
    }

    /// <summary>
    /// Tests connection to FTP server.
    /// </summary>
    public override async Task<bool> TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_host))
            return false;

        try
        {
            var client = await EnsureClientAsync();
            return client.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lists files in a directory on the FTP server.
    /// </summary>
    public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
    {
        ValidateInitialization();
        
        var normalizedPath = NormalizePath(directoryPath);
        var files = new List<string>();
        var client = await EnsureClientAsync();
        
        if (recursive)
        {
            var items = await client.GetListing(normalizedPath, FtpListOption.Recursive);
            foreach (var item in items)
            {
                if (item.Type == FtpObjectType.File)
                {
                    // Remove leading / for consistency with file paths
                    var filePath = item.FullName.TrimStart('/');
                    files.Add(filePath);
                }
            }
        }
        else
        {
            var items = await client.GetListing(normalizedPath);
            foreach (var item in items)
            {
                if (item.Type == FtpObjectType.File)
                {
                    // Remove leading / for consistency with file paths
                    var filePath = item.FullName.TrimStart('/');
                    files.Add(filePath);
                }
            }
        }
        
        return files;
    }

    public override async Task<FileMetadata> StatAsync(string path)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(path);
        var client = await EnsureClientAsync();

        var metadata = new FileMetadata
        {
            Path = path,
            Name = Path.GetFileName(path) ?? path
        };

        try
        {
            var item = await client.GetObjectInfo(normalizedPath);
            if (item != null)
            {
                metadata.Exists = true;
                metadata.IsDirectory = item.Type == FtpObjectType.Directory;
                metadata.Size = item.Size;
                metadata.Modified = item.Modified;
                metadata.Created = item.Created;
            }
            else
            {
                metadata.Exists = false;
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
        var normalizedPath = NormalizePath(path);
        var client = await EnsureClientAsync();

        await client.CreateDirectory(normalizedPath, force: recursive);
    }

    public override async Task CopyAsync(string sourcePath, string destinationPath)
    {
        ValidateInitialization();
        
        // FTP doesn't have native copy, so we download and re-upload
        var sourceNormalized = NormalizePath(sourcePath);
        var destNormalized = NormalizePath(destinationPath);
        var client = await EnsureClientAsync();

        using var stream = new MemoryStream();
        await client.DownloadStream(stream, sourceNormalized);
        stream.Position = 0;
        
        // Ensure destination directory exists
        var destDir = GetDirectoryPath(destNormalized);
        if (!string.IsNullOrEmpty(destDir) && destDir != "/")
        {
            await client.CreateDirectory(destDir, force: true);
        }
        
        await client.UploadStream(stream, destNormalized, createRemoteDir: true);
    }

    public override async Task MoveAsync(string sourcePath, string destinationPath)
    {
        ValidateInitialization();
        var sourceNormalized = NormalizePath(sourcePath);
        var destNormalized = NormalizePath(destinationPath);
        var client = await EnsureClientAsync();

        // Ensure destination directory exists
        var destDir = GetDirectoryPath(destNormalized);
        if (!string.IsNullOrEmpty(destDir) && destDir != "/")
        {
            await client.CreateDirectory(destDir, force: true);
        }

        await client.MoveFile(sourceNormalized, destNormalized);
    }

    public override async Task<bool> ExistsAsync(string path)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(path);
        var client = await EnsureClientAsync();

        return await client.FileExists(normalizedPath) || await client.DirectoryExists(normalizedPath);
    }

    public override async Task<Stream> ReadStreamAsync(string filePath)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(filePath);
        var client = await EnsureClientAsync();
        
        // Download to memory stream and return it
        var stream = new MemoryStream();
        await client.DownloadStream(stream, normalizedPath);
        stream.Position = 0;
        return stream;
    }

    public override async Task WriteStreamAsync(string filePath, Stream content)
    {
        ValidateInitialization();
        var normalizedPath = NormalizePath(filePath);
        var client = await EnsureClientAsync();
        
        // Ensure destination directory exists
        var destDir = GetDirectoryPath(normalizedPath);
        if (!string.IsNullOrEmpty(destDir) && destDir != "/")
        {
            await client.CreateDirectory(destDir, force: true);
        }
        
        await client.UploadStream(content, normalizedPath, createRemoteDir: true);
    }

    /// <summary>
    /// Disposes the cached FTP client.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _clientLock.WaitAsync();
        try
        {
            if (_client != null)
            {
                if (_client.IsConnected)
                {
                    await _client.Disconnect();
                }
                _client.Dispose();
                _client = null;
            }
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
        if (string.IsNullOrWhiteSpace(_host))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
    }

    private async Task<AsyncFtpClient> EnsureClientAsync()
    {
        await _clientLock.WaitAsync();
        try
        {
            if (_client == null || !_client.IsConnected)
            {
                _client?.Dispose();
                _client = new AsyncFtpClient(_host, _username, _password, _port);
                await _client.Connect();
            }
            return _client;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";
        
        path = path.Replace("\\", "/");
        if (!path.StartsWith("/"))
            path = "/" + path;
        
        return path;
    }

    private string GetDirectoryPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        return string.IsNullOrEmpty(directory) ? "/" : directory.Replace("\\", "/");
    }

    #endregion
}
