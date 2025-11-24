using System.Text.Json;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;


/// <summary>
/// Observer Pattern implementation.
/// Manages provider registration, discovery, and notifies observers of changes.
/// </summary>
/// 
public class ProviderManager
{
    private readonly Dictionary<string, Provider> _providers;
    private readonly List<IProviderObserver> _observers;
    private readonly Logger _logger;
    private readonly ProviderFactory _providerFactory;

    private readonly IServiceScopeFactory _scopeFactory;

    public ProviderManager(
        ProviderFactory providerFactory,
        Logger logger,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IProviderObserver> observers)
    {
        _providers = new Dictionary<string, Provider>();
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        //observers list
        _observers = observers.ToList();

        _logger.LogInformation($"[System] ProviderManager initialized with {_observers.Count} observers.");

        foreach (var obs in _observers)
        {
            _logger.LogInformation($"[System] - Observer Loaded: {obs.GetType().Name}");
        }
    }

    /// <summary>
    /// Connects to the DB, fetches active providers, and loads them into memory.
    /// </summary>
    public async Task LoadProvidersFromDatabaseAsync()
    {
        _logger.LogInformation("ProviderManager: Starting database sync...");

        // Create a temporary scope to access the Database Repository
        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
            var entities = await repository.GetActiveProvidersAsync();

            foreach (var entity in entities)
            {
                try
                {
                    //check supported type
                    if (!_providerFactory.IsProviderTypeSupported(entity.Type))
                    {
                        _logger.LogWarning($"Skipping unsupported provider: {entity.Name} ({entity.Type})");
                        continue;
                    }

                    // 2. parse configuration, this is a json string
                    var config = !string.IsNullOrWhiteSpace(entity.Configuration)
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Configuration) ??
                          new Dictionary<string, string>()
                        : new Dictionary<string, string>();

                    // create provider instance
                    var provider = await _providerFactory.CreateProviderAsync(entity.Type, entity.Id, config);

                    // 4. Add to Memory
                    // We use RegisterProvider to ensure observers are notified if needed, 
                    // or add directly to dictionary to avoid noise.
                    if (!_providers.ContainsKey(provider.ProviderId))
                    {
                        _providers.Add(provider.ProviderId, provider);
                        _logger.LogInformation($"Loaded from DB: {entity.Name}");

                        // Notify observers about the loaded provider on startup
                        await NotifyProvidersRegistered(provider.ProviderId, provider.ProviderType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to load provider '{entity.Name}': {ex.Message}");
                }
            }
        }

        _logger.LogInformation($"ProviderManager: Sync complete. Active providers: {_providers.Count}");
    }

    public async Task RegisterProvider(Provider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));

        //in memory update
        if (!_providers.ContainsKey(provider.ProviderId))
        {
            _providers.Add(provider.ProviderId, provider);

            // DB Persistence
            // We create a scope again because RegisterProvider might be called 
            // from a Singleton context or API request.
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();

                var entity = new ProviderEntity
                {
                    Id = provider.ProviderId,
                    Name = provider.ProviderId,
                    Type = provider.ProviderType,
                    IsActive = true,
                    Configuration = JsonSerializer.Serialize(provider.Configuration)
                };

                // Try to update first (upsert pattern)
                // If provider doesn't exist, UpdateAsync throws KeyNotFoundException
                try
                {
                    await repo.UpdateAsync(entity);
                }
                catch (KeyNotFoundException)
                {
                    // Provider doesn't exist in DB, create it
                    await repo.AddAsync(entity);
                }
            }

            _logger.LogInformation($"Provider registered: {provider.ProviderId}");
            await NotifyProvidersRegistered(provider.ProviderId, provider.ProviderType);
        }
    }

    public async Task RemoveProvider(string providerId)
    {
        if (_providers.Remove(providerId))
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
                await repo.DeleteAsync(providerId);
            }

            _logger.LogInformation($"Provider removed: {providerId}");
            await NotifyProviderRemoved(providerId);
        }
    }

    //getter
    public async Task<Provider?> GetProvider(string providerId)
    {
        _providers.TryGetValue(providerId, out var provider);
        return await Task.FromResult(provider);
    }

    public async Task<IEnumerable<Provider>> GetAllProviders()
    {
        return await Task.FromResult(_providers.Values.ToList());
    }

    //Obserever pattern methods


    private async Task NotifyObservers(Func<IProviderObserver, Task> action)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await action(observer);
            }
            catch (Exception ex)
            {
                // Prevent one bad observer from crashing the manager
                _logger.LogError($"Observer {observer.GetType().Name} failed: {ex.Message}");
            }
        }
    }

    public void RegisterObserver(IProviderObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
        
        _observers.Add(observer);
    }

    public void RemoveObserver(IProviderObserver observer)
    {
        _observers.Remove(observer);
    }

    public async Task NotifyProvidersRegistered(string pid, string ptype)
    {
        await NotifyObservers(o => o.OnProviderRegistered(pid, ptype));
    }

    public async Task NotifyProviderRemoved(string pid)
    {
        await NotifyObservers(o => o.OnProviderRemoved(pid));
    }
}
