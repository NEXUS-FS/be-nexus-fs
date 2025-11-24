using FluentAssertions;
using Infrastructure.Services;

namespace NexusFS.Tests;

public class ProviderFactoryTests
{
    private readonly ProviderFactory _factory = new();

    [Fact]
    public void CreateProvider_WithLocalType_ShouldReturnLocalProvider()
    {
        var config = new Dictionary<string, string> { { "basePath", Path.GetTempPath() } };

        var provider = _factory.CreateProvider("Local", "test-local", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<LocalProvider>();
        provider.ProviderId.Should().Be("test-local");
        provider.ProviderType.Should().Be("Local");
    }

    [Fact]
    public void CreateProvider_WithLocalTypeLowerCase_ShouldReturnLocalProvider()
    {
        var config = new Dictionary<string, string> { { "basePath", Path.GetTempPath() } };

        var provider = _factory.CreateProvider("local", "test-local", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<LocalProvider>();
        provider.ProviderId.Should().Be("test-local");
    }

    [Fact]
    public void CreateProvider_WithMemoryType_ShouldReturnMemoryProvider()
    {
        var config = new Dictionary<string, string>();

        var provider = _factory.CreateProvider("Memory", "test-memory", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<MemoryProvider>();
        provider.ProviderId.Should().Be("test-memory");
        provider.ProviderType.Should().Be("Memory");
    }

    [Fact]
    public void CreateProvider_WithMemoryTypeLowerCase_ShouldReturnMemoryProvider()
    {
        var config = new Dictionary<string, string>();

        var provider = _factory.CreateProvider("memory", "test-memory", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<MemoryProvider>();
        provider.ProviderId.Should().Be("test-memory");
    }

    [Fact]
    public void CreateProvider_WithFtpType_ShouldReturnFtpProvider()
    {
        var config = new Dictionary<string, string> 
        { 
            { "host", "ftp.example.com" },
            { "username", "testuser" },
            { "password", "testpass" }
        };

        var provider = _factory.CreateProvider("FTP", "test-ftp", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<FtpProvider>();
        provider.ProviderId.Should().Be("test-ftp");
        provider.ProviderType.Should().Be("FTP");
    }

    [Fact]
    public void CreateProvider_WithFtpTypeLowerCase_ShouldReturnFtpProvider()
    {
        var config = new Dictionary<string, string> 
        { 
            { "host", "ftp.example.com" },
            { "username", "testuser" },
            { "password", "testpass" }
        };

        var provider = _factory.CreateProvider("ftp", "test-ftp", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<FtpProvider>();
        provider.ProviderId.Should().Be("test-ftp");
    }

    [Fact]
    public void CreateProvider_WithUnknownType_ShouldThrowArgumentException()
    {
        var config = new Dictionary<string, string>();

        var exception = Assert.Throws<ArgumentException>(() => 
            _factory.CreateProvider("UnknownType", "test-id", config));
        
        exception.ParamName.Should().Be("providerType");
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public void CreateProvider_WithEmptyType_ShouldThrowArgumentException()
    {
        var config = new Dictionary<string, string>();

        var exception = Assert.Throws<ArgumentException>(() => 
            _factory.CreateProvider("", "test-id", config));
        
        exception.ParamName.Should().Be("providerType");
        exception.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public void CreateProvider_WithWhitespaceType_ShouldThrowArgumentException()
    {
        var config = new Dictionary<string, string>();

        var exception = Assert.Throws<ArgumentException>(() => 
            _factory.CreateProvider("   ", "test-id", config));
        
        exception.ParamName.Should().Be("providerType");
    }

    [Fact]
    public void CreateProvider_WithEmptyId_ShouldThrowArgumentException()
    {
        var config = new Dictionary<string, string>();

        var exception = Assert.Throws<ArgumentException>(() => 
            _factory.CreateProvider("Local", "", config));
        
        exception.ParamName.Should().Be("providerId");
        exception.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public void CreateProvider_WithNullConfig_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            _factory.CreateProvider("Local", "test-id", null!));
    }

    [Fact]
    public async Task CreateProviderAsync_WithLocalType_ShouldReturnInitializedLocalProvider()
    {
        var config = new Dictionary<string, string> { { "basePath", Path.GetTempPath() } };

        var provider = await _factory.CreateProviderAsync("Local", "test-local", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<LocalProvider>();
        provider.ProviderId.Should().Be("test-local");
        provider.Configuration.Should().ContainKey("basePath");
    }

    [Fact]
    public async Task CreateProviderAsync_WithMemoryType_ShouldReturnInitializedMemoryProvider()
    {
        var config = new Dictionary<string, string>();

        var provider = await _factory.CreateProviderAsync("Memory", "test-memory", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<MemoryProvider>();
        provider.ProviderId.Should().Be("test-memory");
    }

    [Fact]
    public async Task CreateProviderAsync_WithFtpType_ShouldReturnInitializedFtpProvider()
    {
        var config = new Dictionary<string, string> 
        { 
            { "host", "ftp.example.com" },
            { "username", "testuser" },
            { "password", "testpass" }
        };

        var provider = await _factory.CreateProviderAsync("FTP", "test-ftp", config);

        provider.Should().NotBeNull();
        provider.Should().BeOfType<FtpProvider>();
        provider.ProviderId.Should().Be("test-ftp");
        provider.Configuration.Should().ContainKey("host");
    }

    [Fact]
    public async Task CreateProviderAsync_WithUnknownType_ShouldThrowArgumentException()
    {
        var config = new Dictionary<string, string>();

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _factory.CreateProviderAsync("UnknownType", "test-id", config));
        
        exception.ParamName.Should().Be("providerType");
        exception.Message.Should().Contain("not supported");
    }

    [Fact]
    public async Task CreateProviderAsync_WithNullConfig_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _factory.CreateProviderAsync("Local", "test-id", null!));
    }

    [Fact]
    public void IsProviderTypeSupported_WithLocal_ShouldReturnTrue()
    {
        var result = _factory.IsProviderTypeSupported("Local");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProviderTypeSupported_WithMemory_ShouldReturnTrue()
    {
        var result = _factory.IsProviderTypeSupported("Memory");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProviderTypeSupported_WithFtp_ShouldReturnTrue()
    {
        var result = _factory.IsProviderTypeSupported("FTP");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProviderTypeSupported_WithFtpLowerCase_ShouldReturnTrue()
    {
        var result = _factory.IsProviderTypeSupported("ftp");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsProviderTypeSupported_WithUnknownType_ShouldReturnFalse()
    {
        var result = _factory.IsProviderTypeSupported("UnknownType");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsProviderTypeSupported_WithEmptyString_ShouldReturnFalse()
    {
        var result = _factory.IsProviderTypeSupported("");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsProviderTypeSupported_WithNull_ShouldReturnFalse()
    {
        var result = _factory.IsProviderTypeSupported(null!);

        result.Should().BeFalse();
    }
}

