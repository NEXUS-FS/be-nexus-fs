using Application.DTOs;
using Domain.Repositories;
using Infrastructure.Services.Observability;

namespace Infrastructure.Services.UI
{
    /// <summary>
    /// Exposes provider configuration and status to the management UI.
    /// </summary>
    public class ProviderUIService
    {

        private readonly IProviderRepository _providerRepository; //we need to access provider data, we do not care about implementation details
        private readonly IProviderObserver _observabilityService; //we need to track events and metrics related to providers (idea we need more of those to support logger and also metrics observer?)

        // Constructor
        public ProviderUIService(
            IProviderRepository providerRepository,
            IProviderObserver observabilityService)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _observabilityService = observabilityService ?? throw new ArgumentNullException(nameof(observabilityService));
        }


        /// Retrieves a provider’s configuration and runtime status. (from DTOs...)
        public async Task<ProviderStatusResponse?> GetProviderConfigAsync(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

            // Log start
            // await _observabilityService.TrackEventAsync("ProviderConfigRequested", new { ProviderId = providerId });..

            // Fetch provider data
            // var provider = await _providerRepository.GetByIdAsync(providerId);
            var provider = await _providerRepository.GetByIdAsync(providerId); //we need to implement this method in repository
            if (provider == null)
            {
                await _observabilityService.TrackEventAsync("ProviderNotFound", new { ProviderId = providerId });
                return null;
            }

            // collect some runtime metrics time based on provider.?
            //var metrics = await _observabilityService.GetProviderMetricsAsync(providerId);

            // Map to DTO
            var response = new ProviderStatusResponse
            {
                //to be implemented...
            };



            return response;
        }


        /// Registers or updates a provider configuration.
        public async Task<bool> RegisterOrUpdateProviderAsync(ProviderRegistrationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //collect metrics and log based on what happens..

            var existing = null;
            //check if provider exists...
            //   existing = await _providerRepository.GetByIdAsync(request.ProviderId); or some method that does that..
            if (existing != null)
            {
                // update existing provider
                throw new NotImplementedException("Provider update not implemented yet.");
            }
            else
            {
                // register new provider
                throw new NotImplementedException("Provider registration not implemented yet.");
            }

            return true;
        }
    }
}
