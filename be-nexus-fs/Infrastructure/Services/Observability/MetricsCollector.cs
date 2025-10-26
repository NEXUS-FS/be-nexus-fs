using Application.DTOs;
using Domain.Repositories;

/// <summary>
/// Observer Pattern implementation for metrics collection.
/// Collects and exposes performance and usage metrics.
/// Implements IProviderObserver to track provider metrics.
/// </summary>

namespace Infrastructure.Services.Observability
{
    public class MetricsCollector : IProviderObserver
    {
        private readonly IMetricRepository _metricRepository;
        private readonly Logger _logger;
        private readonly List<MetricsData> _metricBuffer;

        public MetricsCollector(IMetricRepository metricRepository, Logger logger)
        {
            _metricRepository = metricRepository;
            _metricBuffer = new List<MetricsData>();
            _logger = logger;
        }

        private void LogMetricEvent(string level, string message)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task FlushMetricsAsync()
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        // IProviderObserver implementation
        public async Task OnProviderRegistered(string providerId, string providerType)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderRemoved(string providerId)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderStatusChanged(string providerId, string status)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task OnProviderError(string providerId, Exception exception)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }
    }
}
