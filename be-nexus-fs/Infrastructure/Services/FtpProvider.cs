using FluentFTP;
using Infrastructure.Services.Observability;

namespace Infrastructure.Services;

/// <summary>
/// FTP storage provider implementation.
/// </summary>
public class FtpProvider : Provider, IAsyncDisposable
{
    private string _host = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _port = 21;
    private bool _skipCertificateValidation;
    private FtpEncryptionMode _encryptionMode = FtpEncryptionMode.Auto;
    private AsyncFtpClient? _client;
    private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);
    private readonly Logger? _logger;

    public FtpProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
        : this(providerId, providerType, configuration, null)
    {
    }

    // Convenience constructor for ProviderFactory
    public FtpProvider(string providerId) 
        : this(providerId, null)
    {
    }

    // Internal/testing-friendly constructor with logger injection
    public FtpProvider(string providerId, Logger? logger = null)
        : this(providerId, "FTP", new Dictionary<string, string>(), logger)
    {
    }

    // Full constructor with logger support
    public FtpProvider(string providerId, string providerType, Dictionary<string, string> configuration, Logger? logger) 
        : base(providerId, providerType, configuration)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the FTP provider with configuration.
    /// </summary>
    public override async Task Initialize(Dictionary<string, string> config)
    {
        await Task.CompletedTask;

        Configuration = config ?? throw new ArgumentNullException(nameof(config));

        if (!config.TryGetValue("host", out _host!))
            throw new ArgumentException("host configuration is required for FtpProvider");

        if (!config.TryGetValue("username", out _username!))
            throw new ArgumentException("username configuration is required for FtpProvider");

        if (!config.TryGetValue("password", out _password!))
            throw new ArgumentException("password configuration is required for FtpProvider");

        if (config.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var port))
        {
            _port = port;
        }

        // Certificate validation configuration
        // SECURITY WARNING: Setting skipCertificateValidation to true disables SSL/TLS certificate validation.
        // This makes the connection vulnerable to man-in-the-middle attacks. Only use in development/testing
        // environments with self-signed certificates. In production, always use proper certificates and keep
        // this setting as false (default).
        if (config.TryGetValue("skipCertificateValidation", out var skipCertStr) && 
            bool.TryParse(skipCertStr, out var skipCert))
        {
            _skipCertificateValidation = skipCert;
        }

        // Encryption mode configuration
        // Options: None (plain FTP), Explicit (FTPS with explicit TLS), Implicit (FTPS with implicit TLS), Auto (try encryption if available)
        // Default: Auto (secure, attempts encryption but falls back to plain FTP if not supported)
        if (config.TryGetValue("encryptionMode", out var encryptionModeStr))
        {
            _encryptionMode = encryptionModeStr.ToLowerInvariant() switch
            {
                "none" => FtpEncryptionMode.None,
                "explicit" => FtpEncryptionMode.Explicit,
                "implicit" => FtpEncryptionMode.Implicit,
                "auto" => FtpEncryptionMode.Auto,
                _ => throw new ArgumentException(
                    $"Invalid encryptionMode value: '{encryptionModeStr}'. Valid values are: None, Explicit, Implicit, Auto",
                    nameof(config))
            };
        }

        // Create and cache the FTP client
        await EnsureClientAsync();
    }

    /// <summary>
    /// Creates and configures an async FTP client.
    /// </summary>
    /// <remarks>
    /// SECURITY: Certificate validation is controlled by the skipCertificateValidation configuration.
    /// By default, certificates are validated. Only disable validation in development/testing environments.
    /// Encryption mode is configurable via the encryptionMode setting, defaulting to Auto for secure connections.
    /// </remarks>
    private AsyncFtpClient CreateAsyncFtpClient()
    {
        var client = new AsyncFtpClient(_host, _username, _password, _port);
        client.Config.EncryptionMode = _encryptionMode;
        client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
        
        // Certificate validation: false = validate certificates (secure, default)
        //                        true = skip validation (insecure, only for testing)
        client.Config.ValidateAnyCertificate = _skipCertificateValidation;
        
        return client;
    }

    /// <summary>
    /// Ensures the FTP client is created and connected.
    /// </summary>
    private async Task<AsyncFtpClient> EnsureClientAsync()
    {
        await _clientLock.WaitAsync();
        try
        {
            _client ??= CreateAsyncFtpClient();

            // Reconnect if not connected
            if (!_client.IsConnected)
            {
                try
                {
                    await _client.AutoConnect();
                }
                catch (Exception ex)
                {
                    // Log the connection failure for debugging
                    _logger?.LogError(
                        $"Connection attempt failed for provider {ProviderId} (Host: {_host}:{_port})",
                        nameof(FtpProvider),
                        ex);
                    
                    // If reconnect fails, dispose and create a new client
                    try
                    {
                        await _client.DisposeAsync();
                    }
                    catch (Exception disposeEx)
                    {
                        _logger?.LogError(
                            $"Error disposing client for provider {ProviderId}",
                            nameof(FtpProvider),
                            disposeEx);
                    }
                    
                    _client = CreateAsyncFtpClient();
                    await _client.AutoConnect();
                }
            }

            return _client;
        }
        finally
        {
            _clientLock.Release();
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
        
        // Download to memory stream and read as text
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
        
        // Ensure parent directories exist before uploading (createRemoteDir handles this, but we ensure it works)
        var directoryPath = GetDirectoryPath(normalizedPath);
        if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
        {
            try
            {
                // CreateDirectory with force=true creates parent directories recursively
                await client.CreateDirectory(directoryPath, force: true);
            }
            catch
            {
                // Directory might already exist or creation failed, continue anyway
                // UploadStream with createRemoteDir will try to create if needed
            }
        }
        
        // Upload text content (createRemoteDir ensures directories are created)
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(contentBytes);
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
        catch (Exception ex)
        {
            // Log connection test failure for debugging
            _logger?.LogError(
                $"Connection test failed for provider {ProviderId} (Host: {_host}:{_port})",
                nameof(FtpProvider),
                ex);
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

    /// <summary>
    /// Normalizes a file or directory path for FTP operations.
    /// </summary>
    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == ".")
        {
            return "/";
        }
        
        // Ensure path starts with /
        if (!path.StartsWith("/"))
        {
            path = "/" + path;
        }
        
        return path;
    }

    /// <summary>
    /// Extracts the directory path from a file path.
    /// </summary>
    private string GetDirectoryPath(string filePath)
    {
        var lastSlash = filePath.LastIndexOf('/');
        if (lastSlash <= 0)
            return "/";
        
        return filePath.Substring(0, lastSlash);
    }

    #endregion
}