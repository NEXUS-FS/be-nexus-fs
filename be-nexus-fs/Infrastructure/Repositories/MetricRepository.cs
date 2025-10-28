using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for metrics.
/// </summary>
public class MetricRepository : IMetricRepository
{
    private readonly NexusFSDbContext _context;

    public MetricRepository(NexusFSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MetricEntity> AddAsync(MetricEntity metric)
    {
        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        await _context.Metrics.AddAsync(metric);
        await _context.SaveChangesAsync();
        return metric;
    }

    public async Task<IEnumerable<MetricEntity>> GetByProviderIdAsync(string providerId, DateTime since)
    {
        return await _context.Metrics
            .Where(m => m.ProviderId == providerId && m.Timestamp >= since)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<MetricEntity>> GetByMetricNameAsync(string metricName, DateTime since)
    {
        return await _context.Metrics
            .Where(m => m.MetricName == metricName && m.Timestamp >= since)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<MetricEntity>> GetAllAsync(DateTime since)
    {
        return await _context.Metrics
            .Where(m => m.Timestamp >= since)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }
}