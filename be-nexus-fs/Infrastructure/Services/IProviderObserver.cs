namespace Infrastructure.Services;

/// <summary>
/// Observer Pattern interface for provider event notifications.
/// Implements the Observer pattern for provider lifecycle events.
/// </summary>
public interface IProviderObserver
{
    Task OnProviderRegistered(string providerId, string providerType);
    Task OnProviderRemoved(string providerId);
    Task OnProviderStatusChanged(string providerId, string status);
    Task OnProviderError(string providerId, Exception exception);
    
    // Add these methods for event tracking
    Task TrackEventAsync(string eventName, object data);
    Task<Dictionary<string, object>> GetProviderMetricsAsync(string providerId);
}
