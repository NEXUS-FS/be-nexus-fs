
using Infrastructure.Services.Observability;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Decorator Pattern implementation for caching functionality.
/// Wraps an existing storage provider to add intelligent caching capabilities
/// that improve performance by reducing redundant file operations.
/// 
/// This decorator implements the Gang of Four Decorator pattern, allowing
/// caching behavior to be added dynamically to any Provider implementation
/// without modifying the original provider code.
/// 
/// AOP Cross-Cutting Concerns Applied:
/// - Caching: Automatic cache management for read operations
/// - Performance Monitoring: Cache hit/miss metrics collection
/// - Logging: Cache operation logging for debugging
/// - Error Handling: Cache invalidation on errors
/// 
/// Cache Strategy:
/// - Read operations: Cache file content with configurable TTL
/// - Write operations: Invalidate cache entries and update cache
/// - Delete operations: Remove cached entries
/// - Connection tests: Cache connection status temporarily
/// 
/// Intercepted Operations (AOP Join Points):
/// 1. ReadFileAsync: AROUND advice for cache-first read strategy
/// 2. WriteFileAsync: AFTER advice for cache invalidation
/// 3. DeleteFileAsync: AFTER advice for cache cleanup
/// 4. TestConnectionAsync: AROUND advice for connection status caching
/// </summary>
namespace Infrastructure.Services.Decorators
{
    public class CachedProviderDecorator : Provider
    {
        #region Private Fields
        /// <summary>
        /// The underlying provider being decorated with caching functionality.
        /// </summary>
        private readonly Provider _decoratedProvider;

        /// <summary>
        /// In-memory cache for storing file content and metadata.
        /// Acts as the primary cache layer before falling back to the decorated provider.
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Logger for cache operations and debugging.
        /// AOP Concern: Cross-cutting logging for cache hit/miss tracking.
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Cache configuration settings (TTL, size limits, etc.).
        /// </summary>
        private readonly CacheOptions _cacheOptions;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the CachedProviderDecorator.
        /// 
        /// AOP Integration Point: Constructor injection allows for AOP framework
        /// integration where logging, metrics, and other cross-cutting concerns
        /// can be automatically injected.
        /// </summary>
        /// <param name="decoratedProvider">The provider to wrap with caching</param>
        /// <param name="memoryCache">Cache implementation for storing data</param>
        /// <param name="logger">Logger for cache operations</param>
        /// <param name="providerId">Unique identifier for this provider instance</param>
        /// <param name="providerType">Type classification of the provider</param>
        /// <param name="configuration">Provider configuration dictionary</param>
        public CachedProviderDecorator(
            Provider decoratedProvider,
            IMemoryCache memoryCache,
            Logger logger,
            string providerId,
            string providerType,
            Dictionary<string, string> configuration)
            : base(providerId, providerType, configuration)
        {
            _decoratedProvider = decoratedProvider ?? throw new ArgumentNullException(nameof(decoratedProvider));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheOptions = new CacheOptions(configuration);
        }
        #endregion

        #region Cache-Enhanced Provider Operations
        /// <summary>
        /// Reads a file with intelligent caching.
        /// 
        /// AOP Join Point: AROUND advice pattern
        /// - BEFORE: Check cache for existing content
        /// - PROCEED: Execute underlying provider if cache miss
        /// - AFTER: Store result in cache for future requests
        /// 
        /// Cache Strategy:
        /// 1. Generate cache key from file path and provider ID
        /// 2. Check memory cache first (fastest)
        /// 3. On cache miss, delegate to decorated provider
        /// 4. Cache successful results with configurable TTL
        /// 5. Log cache hit/miss for performance monitoring
        /// 
        /// AOP Aspects Applied:
        /// [LoggingAspect] - Logs cache operations and performance
        /// [MetricsAspect] - Tracks cache hit ratio and response times
        /// [ErrorHandlingAspect] - Handles cache corruption gracefully
        /// </summary>
        /// <param name="filePath">The file path to read</param>
        /// <returns>File content as string, either from cache or underlying provider</returns>
        // AOP Advice Points:
        // [BEFORE] Log cache lookup attempt
        // [AROUND] Measure cache operation time
        // [AFTER] Log cache hit/miss result and update metrics
        // [AFTER_THROWING] Handle cache errors and invalidate corrupted entries
        public override async Task<string> ReadFileAsync(string filePath)
        {
            // AOP BEFORE: Log method entry and cache lookup
            var cacheKey = GenerateCacheKey("file-content", filePath);

            // Cache-first strategy implementation
            if (_memoryCache.TryGetValue(cacheKey, out object? cachedValue) && cachedValue is string cachedContent)
            {
                // AOP AFTER: Log cache hit
                _logger?.LogInformation($"Cache HIT for file: {filePath}", "CachedProviderDecorator");
                return cachedContent;
            }

            // AOP AROUND: Delegate to decorated provider on cache miss
            _logger?.LogInformation($"Cache MISS for file: {filePath}, delegating to provider", "CachedProviderDecorator");

            try
            {
                var content = await _decoratedProvider.ReadFileAsync(filePath);

                // AOP AFTER: Cache the successful result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheOptions.FileCacheTtl,
                    Priority = CacheItemPriority.Normal
                };

                _memoryCache.Set(cacheKey, content, cacheOptions);
                _logger?.LogInformation($"Cached file content for: {filePath}", "CachedProviderDecorator");

                return content;
            }
            catch (Exception ex)
            {
                // AOP AFTER_THROWING: Log error and ensure no corrupt cache entries
                _logger?.LogError($"Error reading file {filePath}, invalidating cache", "CachedProviderDecorator", ex);
                _memoryCache.Remove(cacheKey);
                throw;
            }
        }

        /// <summary>
        /// Writes a file and invalidates related cache entries.
        /// 
        /// AOP Join Point: AFTER advice pattern
        /// - PROCEED: Execute the write operation first
        /// - AFTER: Invalidate cache entries that may be stale
        /// 
        /// Cache Invalidation Strategy:
        /// 1. Execute write operation on decorated provider
        /// 2. Remove specific file cache entry
        /// 3. Optionally invalidate directory-level caches
        /// 4. Log invalidation for debugging
        /// 
        /// AOP Aspects Applied:
        /// [LoggingAspect] - Logs write operations and cache invalidation
        /// [MetricsAspect] - Tracks write performance and cache invalidation frequency
        /// [ErrorHandlingAspect] - Ensures cache consistency on write failures
        /// </summary>
        /// <param name="filePath">The file path to write to</param>
        /// <param name="content">The content to write</param>
        // AOP Advice Points:
        // [BEFORE] Log write operation start
        // [AFTER] Invalidate cache entries and log invalidation
        // [AFTER_THROWING] Handle write errors and maintain cache consistency
        public override async Task WriteFileAsync(string filePath, string content)
        {
            try
            {
                // AOP AROUND: Execute the actual write operation first
                await _decoratedProvider.WriteFileAsync(filePath, content);

                // AOP AFTER: Invalidate cache entries that are now stale
                var cacheKey = GenerateCacheKey("file-content", filePath);
                _memoryCache.Remove(cacheKey);

                _logger?.LogInformation($"File written and cache invalidated for: {filePath}", "CachedProviderDecorator");
            }
            catch (Exception ex)
            {
                // AOP AFTER_THROWING: Ensure cache consistency on write failure
                _logger?.LogError($"Write operation failed for {filePath}, maintaining cache state", "CachedProviderDecorator", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file and cleans up associated cache entries.
        /// 
        /// AOP Join Point: AFTER advice pattern
        /// - PROCEED: Execute the delete operation
        /// - AFTER: Clean up all cache entries related to the deleted file
        /// 
        /// Cache Cleanup Strategy:
        /// 1. Execute delete on decorated provider
        /// 2. Remove file content cache entry
        /// 3. Remove any metadata cache entries
        /// 4. Log cleanup operations
        /// 
        /// AOP Aspects Applied:
        /// [LoggingAspect] - Logs delete operations and cache cleanup
        /// [MetricsAspect] - Tracks delete performance
        /// [ErrorHandlingAspect] - Handles partial delete scenarios
        /// </summary>
        /// <param name="filePath">The file path to delete</param>
        // AOP Advice Points:
        // [BEFORE] Log delete operation start
        // [AFTER] Clean up cache entries and log cleanup
        // [AFTER_THROWING] Handle delete errors appropriately
        public override async Task DeleteFileAsync(string filePath)
        {
            try
            {
                // AOP AROUND: Execute the actual delete operation
                await _decoratedProvider.DeleteFileAsync(filePath);

                // AOP AFTER: Clean up cache entries for the deleted file
                var cacheKey = GenerateCacheKey("file-content", filePath);
                _memoryCache.Remove(cacheKey);

                _logger?.LogInformation($"File deleted and cache cleaned for: {filePath}", "CachedProviderDecorator");
            }
            catch (Exception ex)
            {
                // AOP AFTER_THROWING: Log error but don't clean cache (file might still exist)
                _logger?.LogError($"Delete operation failed for {filePath}, preserving cache", "CachedProviderDecorator", ex);
                throw;
            }
        }

        /// <summary>
        /// Tests connection with cached status for performance.
        /// 
        /// AOP Join Point: AROUND advice pattern
        /// - BEFORE: Check for cached connection status
        /// - PROCEED: Execute connection test if cache expired
        /// - AFTER: Cache the connection test result
        /// 
        /// Connection caching helps reduce frequent network calls
        /// for connection validation while ensuring reasonably fresh status.
        /// 
        /// AOP Aspects Applied:
        /// [LoggingAspect] - Logs connection test operations
        /// [MetricsAspect] - Tracks connection test frequency and cache effectiveness
        /// [ErrorHandlingAspect] - Handles connection failures gracefully
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        // AOP Advice Points:
        // [BEFORE] Log connection test start and check cache
        // [AROUND] Measure connection test time
        // [AFTER] Cache result and log outcome
        // [AFTER_THROWING] Handle connection errors and cache failures
        public override async Task<bool> TestConnectionAsync()
        {
            var cacheKey = GenerateCacheKey("connection-status", ProviderId);

            // Check for cached connection status
            if (_memoryCache.TryGetValue(cacheKey, out object? cachedValue) && cachedValue is bool cachedStatus)
            {
                _logger?.LogInformation($"Connection status cache HIT for provider: {ProviderId}", "CachedProviderDecorator");
                return cachedStatus;
            }

            try
            {
                // AOP AROUND: Execute actual connection test
                _logger?.LogInformation($"Connection status cache MISS for provider: {ProviderId}, testing connection", "CachedProviderDecorator");

                var connectionStatus = await _decoratedProvider.TestConnectionAsync();

                // AOP AFTER: Cache the connection status with shorter TTL
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheOptions.ConnectionCacheTtl,
                    Priority = CacheItemPriority.High // Connection status is critical
                };

                _memoryCache.Set(cacheKey, connectionStatus, cacheOptions);
                _logger?.LogInformation($"Connection status cached for provider: {ProviderId}, status: {connectionStatus}", "CachedProviderDecorator");

                return connectionStatus;
            }
            catch (Exception ex)
            {
                // AOP AFTER_THROWING: Don't cache failed connection attempts
                _logger?.LogError($"Connection test failed for provider: {ProviderId}", "CachedProviderDecorator", ex);
                throw;
            }
        }

        /// <summary>
        /// Initializes the decorated provider and sets up cache configuration.
        /// 
        /// AOP Join Point: AROUND advice pattern
        /// - BEFORE: Log initialization start
        /// - PROCEED: Initialize decorated provider
        /// - AFTER: Configure cache settings and log completion
        /// 
        /// AOP Aspects Applied:
        /// [LoggingAspect] - Logs initialization progress
        /// [ErrorHandlingAspect] - Handles initialization failures
        /// </summary>
        /// <param name="config">Configuration dictionary for initialization</param>
        // AOP Advice Points:
        // [BEFORE] Log initialization start
        // [AFTER] Log initialization completion
        // [AFTER_THROWING] Handle initialization errors
        public override async Task Initialize(Dictionary<string, string> config)
        {
            try
            {
                _logger?.LogInformation($"Initializing cached provider decorator for: {ProviderId}", "CachedProviderDecorator");

                // AOP AROUND: Initialize the decorated provider
                await _decoratedProvider.Initialize(config);

                // AOP AFTER: Configure cache-specific settings
                _cacheOptions.UpdateFromConfig(config);

                _logger?.LogInformation($"Cached provider decorator initialized successfully for: {ProviderId}", "CachedProviderDecorator");
            }
            catch (Exception ex)
            {
                // AOP AFTER_THROWING: Log initialization failure
                _logger?.LogError($"Failed to initialize cached provider decorator for: {ProviderId}", "CachedProviderDecorator", ex);
                throw;
            }
        }
        #endregion

        #region Cache Management Utilities
        /// <summary>
        /// Generates a consistent cache key for the given operation and parameters.
        /// 
        /// AOP Integration: This method supports cache key generation for
        /// intercepted operations, ensuring consistent caching across all
        /// decorated provider operations.
        /// </summary>
        /// <param name="operation">The operation type (e.g., "file-content", "connection-status")</param>
        /// <param name="identifier">The unique identifier (e.g., file path, provider ID)</param>
        /// <returns>A unique cache key string</returns>
        private string GenerateCacheKey(string operation, string identifier)
        {
            var fnv1a = new Infrastructure.Cache.Hashing.FNV1a64();
            fnv1a.Update(ProviderId);
            fnv1a.Update(operation);
            fnv1a.Update(identifier);
            return fnv1a.Digest().ToString("x16");
        }

        /// <summary>
        /// Clears all cache entries for this provider instance.
        /// 
        /// This method can be called by AOP aspects or external cache management
        /// systems to force cache invalidation when needed.
        /// </summary>
        public void ClearCache()
        {
            // Note: IMemoryCache doesn't provide a way to clear entries by pattern
            // In a production implementation, this would use a more sophisticated
            // cache implementation or maintain a list of cache keys
            _logger?.LogInformation($"Cache clear requested for provider: {ProviderId}", "CachedProviderDecorator");
        }
        #endregion

        #region Cache Configuration
        /// <summary>
        /// Configuration options for cache behavior.
        /// 
        /// This class encapsulates cache-related settings and provides
        /// AOP-friendly configuration management.
        /// </summary>
        private class CacheOptions
        {
            public TimeSpan FileCacheTtl { get; private set; } = TimeSpan.FromMinutes(15);
            public TimeSpan ConnectionCacheTtl { get; private set; } = TimeSpan.FromMinutes(5);

            public CacheOptions(Dictionary<string, string> configuration)
            {
                UpdateFromConfig(configuration);
            }

            public void UpdateFromConfig(Dictionary<string, string> configuration)
            {
                if (configuration.TryGetValue("cache.file.ttl", out string? fileTtl) && !string.IsNullOrEmpty(fileTtl))
                {
                    if (TimeSpan.TryParse(fileTtl, out var parsedFileTtl))
                        FileCacheTtl = parsedFileTtl;
                }

                if (configuration.TryGetValue("cache.connection.ttl", out string? connectionTtl) && !string.IsNullOrEmpty(connectionTtl))
                {
                    if (TimeSpan.TryParse(connectionTtl, out var parsedConnectionTtl))
                        ConnectionCacheTtl = parsedConnectionTtl;
                }
            }
        }
        #endregion
    }
}
