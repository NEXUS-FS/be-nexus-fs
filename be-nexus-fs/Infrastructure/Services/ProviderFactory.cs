
namespace Infrastructure.Services;

public class ProviderFactory
{
    public async Task<Provider> CreateProviderAsync(string providerType, string providerId,
        Dictionary<string, string> configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var provider = InstantiateProvider(providerType, providerId);
        await provider.Initialize(configuration);
        return provider;
    }

    public Provider CreateProvider(string providerType, string providerId, Dictionary<string, string> configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var provider = InstantiateProvider(providerType, providerId);

        // Sync wait is safe here for tests/startup
        provider.Initialize(configuration).GetAwaiter().GetResult();
        return provider;
    }

    private Provider InstantiateProvider(string providerType, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be empty", nameof(providerType));

        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));

        return providerType.ToLowerInvariant() switch
        {
            "local" or "filesystem" => new LocalProvider(providerId),
            "memory" => new MemoryProvider(providerId),
            "s3" or "aws" => new S3Provider(providerId),
            "ftp" => new FtpProvider(providerId),
            _ => throw new ArgumentException($"Provider type '{providerType}' is not supported.", nameof(providerType))
        };
    }

    public IEnumerable<string> GetSupportedProviderTypes() => new[] { "Local", "Memory", "S3", "FTP" };

    public bool IsProviderTypeSupported(string providerType)
    {
        if (string.IsNullOrWhiteSpace(providerType)) return false;
        return providerType.ToLowerInvariant() is "local" or "filesystem" or "memory" or "s3" or "aws" or "ftp";
    }
}