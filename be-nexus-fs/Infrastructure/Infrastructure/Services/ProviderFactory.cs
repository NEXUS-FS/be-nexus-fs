using Infrastructure.Services.Observability;

namespace Infrastructure.Services;

/// <summary>
/// Factory Method / Abstract Factory Pattern.
/// Creates instances of Provider subclasses dynamically based on provider type.
/// </summary>
public class ProviderFactory
{
    private readonly Logger _logger;

    public ProviderFactory(Logger logger)
    {
        _logger = logger;
    }

    public Provider CreateProvider(string providerType, string providerId)
    {
        throw new NotImplementedException();
    }

    public Provider CreateProvider(string providerType, string providerId, Dictionary<string, string> configuration)
    {
        throw new NotImplementedException();
    }

    private Provider InstantiateProvider(string providerType, string providerId)
    {
        throw new NotImplementedException();
    }
}
