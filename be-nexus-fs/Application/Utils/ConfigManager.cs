using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace Application.Utils;

/// <summary>
/// Manages application configuration and settings.
/// Uses ASP.NET Core IConfiguration for centralized configuration access.
/// </summary>
public class ConfigManager
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, object> _cache;

    // Public constructor for DI
    public ConfigManager(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = new ConcurrentDictionary<string, object>();
    }

    /// <summary>
    /// Returns a configuration value for the given key.
    /// </summary>
    public T GetConfigValue<T>(string key)
    {
        // Check cache first
        if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
        {
            return typedValue;
        }

        // Get from configuration
        var value = _configuration.GetValue<T>(key);
        
        if (value != null)
        {
            _cache.TryAdd(key, value);
        }

        return value ?? default!;
    }

    /// <summary>
    /// Returns a configuration value with a default fallback.
    /// </summary>
    public T GetConfigValue<T>(string key, T defaultValue)
    {
        try
        {
            var value = _configuration.GetValue<T>(key);
            return value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets/overrides a configuration value (runtime overrides, tests).
    /// </summary>
    public void SetConfiguration<T>(string key, T value)
    {
        if (value != null)
        {
            _cache.AddOrUpdate(key, value, (k, oldValue) => value);
        }
    }

    /// <summary>
    /// Binds an entire section to a typed object (e.g., JwtOptions).
    /// </summary>
    public T BindSection<T>(string sectionKey) where T : new()
    {
        // Check cache first
        if (_cache.TryGetValue($"section:{sectionKey}", out var cachedSection) && cachedSection is T typedSection)
        {
            return typedSection;
        }

        // Bind from configuration
        var section = new T();
        _configuration.GetSection(sectionKey).Bind(section);
        
        _cache.TryAdd($"section:{sectionKey}", section);
        
        return section;
    }

    /// <summary>
    /// Gets a connection string by name.
    /// </summary>
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) ?? string.Empty;
    }

    /// <summary>
    /// Clears the configuration cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
