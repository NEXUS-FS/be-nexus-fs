namespace Infrastructure.Services;

/// <summary>
/// Factory Method / Abstract Factory Pattern.
/// Creates instances of Provider subclasses dynamically based on provider type.
/// NO DEPENDENCIES - Pure factory pattern.
/// </summary>
public class ProviderFactory
{
    // Parameterless constructor - NO dependencies
    public ProviderFactory()
    {
    }

    /// <summary>
    /// Creates a provider instance based on type and ID.
    /// </summary>
    public Provider CreateProvider(string providerType, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be null or empty.", nameof(providerType));

        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

        return InstantiateProvider(providerType, providerId);
    }

    /// <summary>
    /// Creates a provider instance with configuration (async).
    /// </summary>
    public async Task<Provider> CreateProviderAsync(string providerType, string providerId, Dictionary<string, string> configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var provider = InstantiateProvider(providerType, providerId);
        await provider.Initialize(configuration);
        
        return provider;
    }

    /// <summary>
    /// Creates a provider instance with configuration (sync).
    /// </summary>
    public Provider CreateProvider(string providerType, string providerId, Dictionary<string, string> configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var provider = InstantiateProvider(providerType, providerId);
        provider.Initialize(configuration).Wait();
        
        return provider;
    }

    /// <summary>
    /// Instantiates the appropriate provider based on type.
    /// </summary>
    private Provider InstantiateProvider(string providerType, string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be empty", nameof(providerType));

        return providerType.ToLowerInvariant() switch
        {
            "local" or "filesystem" => new LocalProvider(providerId),
            "ftp" => new FtpProvider(providerId),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported. Supported types: Local, FTP")
        };
    }

    /// <summary>
    /// Gets a list of all supported provider types.
    /// </summary>
    public IEnumerable<string> GetSupportedProviderTypes()
    {
        return new[] { "Local", "FTP" };
    }

    /// <summary>
    /// Validates if a provider type is supported.
    /// </summary>
    public bool IsProviderTypeSupported(string providerType)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            return false;

        return providerType.ToLowerInvariant() switch
        {
            "local" or "filesystem" or "ftp" => true,
            _ => false
        };
    }
}