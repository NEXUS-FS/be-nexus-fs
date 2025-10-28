using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for metric operations.
/// </summary>
public interface IMetricRepository
{
    Task<MetricEntity> AddAsync(MetricEntity metric);
    Task<IEnumerable<MetricEntity>> GetByProviderIdAsync(string providerId, DateTime since);
    Task<IEnumerable<MetricEntity>> GetByMetricNameAsync(string metricName, DateTime since);
    Task<IEnumerable<MetricEntity>> GetAllAsync(DateTime since);
}
