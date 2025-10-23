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
    }
}
