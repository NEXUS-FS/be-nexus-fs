using Infrastructure.Services.Observability;
using System.Data.Common;

namespace Infrastructure.Services
/// <summary>
/// Observer Pattern implementation.
/// Manages provider registration, discovery, and notifies observers of changes.
/// </summary>

{
    public class ProviderManager
    {
        private readonly Dictionary<string, Provider> _providers;
        private readonly List<IProviderObserver> _observers;
        private readonly Logger _logger;
        private readonly ProviderFactory _providerFactory;

        public ProviderManager(ProviderFactory providerFactory, Logger logger)
        {
            _providers = new Dictionary<string, Provider>();
            _observers = new List<IProviderObserver>();
            _providerFactory = providerFactory;
            _logger = logger;
        }

        public void RegisterObserver(IProviderObserver observer)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void RemoveObserver(IProviderObserver observer)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task NotifyProvidersRegistered(string providerId, string providerType)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task NotifyProviderRemoved(string providerId)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task RegisterProvider(Provider provider)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task<Provider> GetProvider(string providerId)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Provider>> GetAllProviders()
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task RemoveProvider(string providerId)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }
    }
}
