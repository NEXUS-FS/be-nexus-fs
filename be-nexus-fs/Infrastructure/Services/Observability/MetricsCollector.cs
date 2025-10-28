using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services;

namespace Infrastructure.Services.Observability
{
    /// <summary>
    /// Observer Pattern implementation for metrics collection.
    /// </summary>
    public class MetricsCollector : IProviderObserver
    {
        private readonly IMetricRepository _metricRepository;
        private readonly Logger _logger;
        private readonly List<MetricEntity> _metricBuffer;
        private readonly object _bufferLock = new();

        public MetricsCollector(IMetricRepository metricRepository, Logger logger)
        {
            _metricRepository = metricRepository ?? throw new ArgumentNullException(nameof(metricRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricBuffer = new List<MetricEntity>();
        }

        #region Metrics Recording Methods

        public async Task RecordMetricAsync(string name, double value, string? unit = null,
            string? providerId = null, string? providerType = null)
        {
            var metric = new MetricEntity
            {
                MetricName = name,
                Value = value,
                Unit = unit ?? "count",
                ProviderId = providerId,
                ProviderType = providerType,
                Timestamp = DateTime.UtcNow
            };

            lock (_bufferLock)
            {
                _metricBuffer.Add(metric);
            }

            // Auto-flush if buffer gets too large
            if (_metricBuffer.Count >= 100)
            {
                await FlushMetricsAsync();
            }
        }

        public async Task RecordOperationDurationAsync(string operationName, TimeSpan duration,
            string? providerId = null, string? providerType = null)
        {
            await RecordMetricAsync(
                $"operation.{operationName}.duration",
                duration.TotalMilliseconds,
                "ms",
                providerId,
                providerType
            );
        }

        public async Task IncrementCounterAsync(string name, int amount = 1,
            string? providerId = null, string? providerType = null)
        {
            await RecordMetricAsync(
                $"counter.{name}",
                amount,
                "count",
                providerId,
                providerType
            );
        }

        public async Task<IEnumerable<MetricEntity>> GetMetricsAsync(
            string? providerId = null, string? providerType = null)
        {
            List<MetricEntity> bufferMetrics;
            lock (_bufferLock)
            {
                bufferMetrics = _metricBuffer.ToList();
            }

            var filteredMetrics = bufferMetrics.AsEnumerable();

            if (!string.IsNullOrEmpty(providerId))
                filteredMetrics = filteredMetrics.Where(m => m.ProviderId == providerId);

            if (!string.IsNullOrEmpty(providerType))
                filteredMetrics = filteredMetrics.Where(m => m.ProviderType == providerType);

            return await Task.FromResult(filteredMetrics);
        }

        public async Task FlushMetricsAsync()
        {
            List<MetricEntity> metricsToFlush;

            lock (_bufferLock)
            {
                if (_metricBuffer.Count == 0)
                    return;

                metricsToFlush = new List<MetricEntity>(_metricBuffer);
                _metricBuffer.Clear();
            }

            foreach (var metric in metricsToFlush)
            {
                await _metricRepository.AddAsync(metric);
            }
        }

        #endregion

        #region IProviderObserver Implementation

        public async Task OnProviderRegistered(string providerId, string providerType)
        {
            await RecordMetricAsync("provider.registered", 1, "count", providerId, providerType);
        }

        public async Task OnProviderRemoved(string providerId)
        {
            await RecordMetricAsync("provider.removed", 1, "count", providerId);
        }

        public async Task OnProviderStatusChanged(string providerId, string status)
        {
            await RecordMetricAsync("provider.status.changed", 1, "count", providerId);
        }

        public async Task OnProviderError(string providerId, Exception exception)
        {
            await RecordMetricAsync("provider.error", 1, "count", providerId);
        }

        public async Task TrackEventAsync(string eventName, object data)
        {
            await RecordMetricAsync($"event.{eventName}", 1, "count");
        }

        public async Task<Dictionary<string, object>> GetProviderMetricsAsync(string providerId)
        {
            var metrics = await GetMetricsAsync(providerId: providerId);
            var metricsList = metrics.ToList();

            var result = new Dictionary<string, object>
            {
                { "ProviderId", providerId },
                { "TotalMetrics", metricsList.Count },
                { "LastUpdated", DateTime.UtcNow }
            };

            var groupedMetrics = metricsList
                .GroupBy(m => m.MetricName)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        Sum = g.Sum(m => m.Value),
                        Average = g.Average(m => m.Value),
                        Min = g.Min(m => m.Value),
                        Max = g.Max(m => m.Value),
                        LastValue = g.OrderByDescending(m => m.Timestamp).First().Value
                    }
                );

            result.Add("Metrics", groupedMetrics);

            return result;
        }

        #endregion
    }
}