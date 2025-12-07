using System.Net;

namespace Infrastructure.Services;

/// <summary>
/// FTP storage provider implementation.
/// </summary>
public class FtpProvider : Provider
{
    private string _host = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _port = 21;

    public FtpProvider(string providerId, string providerType, Dictionary<string, string> configuration) 
        : base(providerId, providerType, configuration)
    {
    }

    // Convenience constructor for ProviderFactory
    public FtpProvider(string providerId) 
        : base(providerId, "FTP", new Dictionary<string, string>())
    {
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

        var ftpUri = GetFtpUri(filePath);
        var request = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.DownloadFile);

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        using var stream = response.GetResponseStream();
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Writes content to a file on FTP server.
    /// </summary>
    public override async Task WriteFileAsync(string filePath, string content)
    {
        ValidateInitialization();

        var ftpUri = GetFtpUri(filePath);
        var request = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.UploadFile);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        request.ContentLength = contentBytes.Length;

        using var requestStream = await request.GetRequestStreamAsync();
        await requestStream.WriteAsync(contentBytes, 0, contentBytes.Length);

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        // File uploaded successfully
    }

    /// <summary>
    /// Deletes a file from FTP server.
    /// </summary>
    public override async Task DeleteFileAsync(string filePath)
    {
        ValidateInitialization();

        var ftpUri = GetFtpUri(filePath);
        var request = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.DeleteFile);

        using var response = (FtpWebResponse)await request.GetResponseAsync();
        // File deleted successfully
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
            var ftpUri = new Uri($"ftp://{_host}:{_port}/");
            var request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(_username, _password);
            request.Timeout = 5000;

            using var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.StatusCode == FtpStatusCode.OpeningData || 
                   response.StatusCode == FtpStatusCode.DataAlreadyOpen;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a file exists on the FTP server.
    /// </summary>
    public override Task<bool> ExistsAsync(string filePath)
    {
        throw new NotImplementedException("ExistsAsync is not yet implemented for FtpProvider");
    }

    /// <summary>
    /// Gets file metadata/statistics from the FTP server.
    /// </summary>
    public override Task<Dictionary<string, object>> StatAsync(string filePath)
    {
        throw new NotImplementedException("StatAsync is not yet implemented for FtpProvider");
    }

    #region Private Helper Methods

    private void ValidateInitialization()
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new InvalidOperationException("Provider not initialized. Call Initialize() first.");
    }

    private Uri GetFtpUri(string filePath)
    {
        var path = filePath.TrimStart('/');
        return new Uri($"ftp://{_host}:{_port}/{path}");
    }

    private FtpWebRequest CreateFtpRequest(Uri ftpUri, string method)
    {
        var request = (FtpWebRequest)WebRequest.Create(ftpUri);
        request.Method = method;
        request.Credentials = new NetworkCredential(_username, _password);
        request.UseBinary = true;
        request.KeepAlive = false;
        return request;
    }

public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
{
    ValidateInitialization();
    
    var files = new List<string>();
    await ListFilesRecursiveAsync(directoryPath, recursive, files);
    return files;
}

private async Task ListFilesRecursiveAsync(string directoryPath, bool recursive, List<string> files)
{
    var ftpUri = GetFtpUri(directoryPath);
    var request = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.ListDirectoryDetails);

    using var response = (FtpWebResponse)await request.GetResponseAsync();
    using var stream = response.GetResponseStream();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var fileName = ParseFtpListLine(line);
        if (!string.IsNullOrEmpty(fileName) && fileName != "." && fileName != "..")
        {
            var fullPath = string.IsNullOrEmpty(directoryPath) 
                ? fileName 
                : $"{directoryPath}/{fileName}";

            if (!line.StartsWith("d"))
            {
                files.Add(fullPath);
            }
            else if (recursive)
            {
                await ListFilesRecursiveAsync(fullPath, true, files);
            }
        }
    }
}

private string ParseFtpListLine(string line)
{
    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    return parts.Length > 0 ? parts[^1] : string.Empty;
}


    #endregion
}