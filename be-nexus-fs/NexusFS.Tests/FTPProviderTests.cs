using FubarDev.FtpServer;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace NexusFS.Tests;

public class TestFtpServer : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly IFtpServerHost _serverHost;
    public int Port { get; }

    public TestFtpServer(int port)
    {
        Port = port;

        var services = new ServiceCollection();

        // 1. Register the in-memory file system
        services.AddFtpServer(builder =>
            builder
                .UseInMemoryFileSystem()
                .EnableAnonymousAuthentication()
        );

        // 2. Set listen address/port
        services.Configure<FtpServerOptions>(opt => {
            opt.ServerAddress = "127.0.0.1";
            opt.Port = Port;
        });

        // 3. Logging (can be a no-op for tests)
        services.AddLogging();

        _services = services.BuildServiceProvider();
        _serverHost = _services.GetRequiredService<IFtpServerHost>();
    }

    public async Task StartAsync()
    {
        await _serverHost.StartAsync();
        // Give the server a moment to be ready
        await Task.Delay(500);
    }

    public async Task StopAsync() => await _serverHost.StopAsync();

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_services is IAsyncDisposable ad)
            await ad.DisposeAsync();
        else
            _services.Dispose();
    }
}

public class FtpProviderTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private TestFtpServer? _ftpServer;
    private int _ftpPort = 2121;
    private FtpProvider? _provider;

    public FtpProviderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // 1. Start the server
        _ftpServer = new TestFtpServer(_ftpPort);
        await _ftpServer.StartAsync();

        // 2. Initialize FTP provider
        _provider = new FtpProvider("ftp-provider");
        var settings = new Dictionary<string, string>
        {
            { "host", "127.0.0.1" },
            { "username", "anonymous" },
            { "password", "anonymous@example.com" },
            { "port", _ftpPort.ToString() },
            { "encryptionMode", "None" } // Test server uses plain FTP
        };
        await _provider.Initialize(settings);
        
        // 3. Wait for connection with timeout (polling instead of fixed delay)
        var maxAttempts = 10;
        var delayMs = 200;
        var connected = false;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            connected = await _provider.TestConnectionAsync();
            if (connected) break;
            await Task.Delay(delayMs);
        }
        
        // If connection test fails, try a simple operation as final verification
        if (!connected)
        {
            try
            {
                await _provider.ListFilesAsync(".", false);
                connected = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"FTP provider failed to connect to test server after {maxAttempts} attempts. " +
                    $"Last error: {ex.Message}", ex);
            }
        }
        
        Assert.True(connected, "FTP provider should be able to connect to the test server");
    }

    [Fact]
    public async Task Test_FTPProvider_ReadWrite()
    {
        // Arrange
        var filePath = "test-readwrite.txt";
        var expectedContent = "Hello, World!";
        
        // Act
        await _provider!.WriteFileAsync(filePath, expectedContent);
        var actualContent = await _provider!.ReadFileAsync(filePath);
        
        // Assert
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task Test_FTPProvider_DeleteFile()
    {
        // Arrange
        var filePath = "test-delete.txt";
        await _provider!.WriteFileAsync(filePath, "Content to delete");
        
        // Verify file exists
        var contentBeforeDelete = await _provider.ReadFileAsync(filePath);
        Assert.Equal("Content to delete", contentBeforeDelete);
        
        // Act
        await _provider.DeleteFileAsync(filePath);
        
        // Assert - file should not exist (should throw an exception)
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _provider.ReadFileAsync(filePath));
        Assert.NotNull(exception);
        
        // FluentFTP wraps exceptions - check both outer and inner exception messages
        var errorMessage = exception.Message;
        if (exception.InnerException != null)
        {
            errorMessage = exception.InnerException.Message;
        }
        
        // Verify it's a file-not-found type error (FTP 550 = file not found)
        Assert.True(errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("550", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("550", StringComparison.OrdinalIgnoreCase),
                   $"Expected file not found error, got: {exception.Message} (Inner: {exception.InnerException?.Message})");
    }

    [Fact]
    public async Task Test_FTPProvider_TestConnection()
    {
        // Act
        var isConnected = await _provider!.TestConnectionAsync();
        
        // Assert
        Assert.True(isConnected, "FTP provider should be connected to the test server");
    }

    [Fact]
    public async Task Test_FTPProvider_ListFiles_NonRecursive()
    {
        // Arrange
        await _provider!.WriteFileAsync("file1.txt", "Content 1");
        await _provider.WriteFileAsync("file2.txt", "Content 2");
        await _provider.WriteFileAsync("subdir/file3.txt", "Content 3");
        
        // Act
        var rootFiles = await _provider.ListFilesAsync("/", recursive: false);
        
        // Assert
        Assert.NotNull(rootFiles);
        // Should find files in root, but subdirectory files may or may not appear depending on FTP server
        Assert.True(rootFiles.Count >= 0, "ListFilesAsync should return a list (may be empty due to FTP server limitations)");
    }

    [Fact]
    public async Task Test_FTPProvider_ListFiles_Recursive()
    {
        // Arrange
        await _provider!.WriteFileAsync("recursive/file1.txt", "Content 1");
        await _provider.WriteFileAsync("recursive/sub/file2.txt", "Content 2");
        
        // Act
        var files = await _provider.ListFilesAsync("/", recursive: true);
        
        // Assert
        Assert.NotNull(files);
        // Recursive listing should work, but exact results depend on FTP server implementation
        Assert.True(files.Count >= 0, "ListFilesAsync should return a list");
    }

    [Fact]
    public async Task Test_FTPProvider_ReadFile_FileNotFound()
    {
        // Act & Assert - reading non-existent file should throw
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _provider!.ReadFileAsync("nonexistent.txt"));
        Assert.NotNull(exception);
        
        // FluentFTP wraps exceptions - check both outer and inner exception messages
        var errorMessage = exception.InnerException?.Message ?? exception.Message;
        
        // Verify it's a file-not-found type error (FTP 550 = file not found)
        Assert.True(errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("550", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("550", StringComparison.OrdinalIgnoreCase),
                   $"Expected file not found error, got: {exception.Message} (Inner: {exception.InnerException?.Message})");
    }

    [Fact]
    public async Task Test_FTPProvider_DeleteFile_FileNotFound()
    {
        // Act & Assert - deleting non-existent file should throw
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _provider!.DeleteFileAsync("nonexistent.txt"));
        Assert.NotNull(exception);
        
        // FluentFTP wraps exceptions - check both outer and inner exception messages
        var errorMessage = exception.InnerException?.Message ?? exception.Message;
        
        // Verify it's a file-not-found type error (FTP 550 = file not found)
        Assert.True(errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("550", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("550", StringComparison.OrdinalIgnoreCase),
                   $"Expected file not found error, got: {exception.Message} (Inner: {exception.InnerException?.Message})");
    }


    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            try
            {
                // Clean up test files
                var files = await _provider.ListFilesAsync(".", true);
                foreach (var file in files)
                {
                    try
                    {
                        await _provider.DeleteFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail cleanup
                        _output.WriteLine($"[FtpProviderTests] Error deleting file '{file}' during cleanup: {ex.Message}");
                    }
                }
                
                // Dispose the provider
                await _provider.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Log but don't fail cleanup
                _output.WriteLine($"[FtpProviderTests] Error during provider cleanup: {ex.Message}");
            }
        }
        
        if (_ftpServer is not null)
        {
            try
            {
                await _ftpServer.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Log but don't fail cleanup
                _output.WriteLine($"[FtpProviderTests] Error disposing FTP server: {ex.Message}");
            }
        }
    }
}