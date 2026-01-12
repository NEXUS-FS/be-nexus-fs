using Infrastructure.Services.Observability;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;
using Domain.Models;

namespace Infrastructure.Services.Decorators
{
    /// <summary>
    /// Redis-based distributed caching decorator for providers.
    /// Uses Redis for caching across multiple application instances.
    /// </summary>
    public class RedisProviderDecorator : Provider
    {
        private readonly Provider _decoratedProvider;
        private readonly IDistributedCache _distributedCache;
        private readonly Logger? _logger;
        private readonly TimeSpan _defaultExpiration;

        public RedisProviderDecorator(
            Provider decoratedProvider,
            IDistributedCache distributedCache,
            Logger? logger = null,
            TimeSpan? defaultExpiration = null)
            : base(decoratedProvider.ProviderId, decoratedProvider.ProviderType, decoratedProvider.Configuration)
        {
            _decoratedProvider = decoratedProvider ?? throw new ArgumentNullException(nameof(decoratedProvider));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(15);
        }

        public Provider DecoratedProvider => _decoratedProvider;

        public override async Task<string> ReadFileAsync(string filePath)
        {
            var cacheKey = GenerateCacheKey("file", filePath);
            
            // Try to get from cache
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger?.LogInformation($"Redis Cache HIT for file: {filePath}", "RedisProviderDecorator");
                return cachedValue;
            }

            _logger?.LogInformation($"Redis Cache MISS for file: {filePath}", "RedisProviderDecorator");
            
            // Get from provider and cache
            var content = await _decoratedProvider.ReadFileAsync(filePath);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
            
            await _distributedCache.SetStringAsync(cacheKey, content, options);
            
            return content;
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            await _decoratedProvider.WriteFileAsync(filePath, content);
            
            // Invalidate cache
            var cacheKey = GenerateCacheKey("file", filePath);
            await _distributedCache.RemoveAsync(cacheKey);
            
            _logger?.LogInformation($"File written and Redis cache invalidated for: {filePath}", "RedisProviderDecorator");
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            await _decoratedProvider.DeleteFileAsync(filePath);
            
            // Invalidate cache
            var cacheKey = GenerateCacheKey("file", filePath);
            await _distributedCache.RemoveAsync(cacheKey);
            
            _logger?.LogInformation($"File deleted and Redis cache invalidated for: {filePath}", "RedisProviderDecorator");
        }

        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            var cacheKey = GenerateCacheKey($"list-{recursive}", directoryPath);
            
            // Try to get from cache
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger?.LogInformation($"Redis Cache HIT for directory listing: {directoryPath}", "RedisProviderDecorator");
                return JsonSerializer.Deserialize<List<string>>(cachedValue) ?? new List<string>();
            }

            _logger?.LogInformation($"Redis Cache MISS for directory listing: {directoryPath}", "RedisProviderDecorator");
            
            // Get from provider and cache
            var files = await _decoratedProvider.ListFilesAsync(directoryPath, recursive);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Shorter TTL for listings
            };
            
            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(files), options);
            
            return files;
        }

        public override async Task<bool> TestConnectionAsync()
        {
            return await _decoratedProvider.TestConnectionAsync();
        }

        public override async Task Initialize(Dictionary<string, string> config)
        {
            await _decoratedProvider.Initialize(config);
            
            // Update our base configuration
            foreach (var kvp in config)
            {
                Configuration[kvp.Key] = kvp.Value;
            }
        }

        public override async Task<FileMetadata> StatAsync(string path)
        {
            var cacheKey = GenerateCacheKey("stat", path);
            
            // Try to get from cache
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger?.LogInformation($"Redis Cache HIT for stat: {path}", "RedisProviderDecorator");
                return JsonSerializer.Deserialize<FileMetadata>(cachedValue)!;
            }

            _logger?.LogInformation($"Redis Cache MISS for stat: {path}", "RedisProviderDecorator");
            
            var metadata = await _decoratedProvider.StatAsync(path);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(metadata), options);
            
            return metadata;
        }

        public override async Task MkdirAsync(string path, bool recursive = true)
        {
            await _decoratedProvider.MkdirAsync(path, recursive);
        }

        public override async Task CopyAsync(string sourcePath, string destinationPath)
        {
            await _decoratedProvider.CopyAsync(sourcePath, destinationPath);
            
            // Invalidate cache for destination
            var destCacheKey = GenerateCacheKey("file", destinationPath);
            await _distributedCache.RemoveAsync(destCacheKey);
        }

        public override async Task MoveAsync(string sourcePath, string destinationPath)
        {
            await _decoratedProvider.MoveAsync(sourcePath, destinationPath);
            
            // Invalidate cache for both paths
            var sourceCacheKey = GenerateCacheKey("file", sourcePath);
            var destCacheKey = GenerateCacheKey("file", destinationPath);
            await Task.WhenAll(
                _distributedCache.RemoveAsync(sourceCacheKey),
                _distributedCache.RemoveAsync(destCacheKey)
            );
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            return await _decoratedProvider.ExistsAsync(path);
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            // Streams are not cached - they're typically for large files
            return await _decoratedProvider.ReadStreamAsync(filePath);
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            await _decoratedProvider.WriteStreamAsync(filePath, content);
            
            // Invalidate cache
            var cacheKey = GenerateCacheKey("file", filePath);
            await _distributedCache.RemoveAsync(cacheKey);
        }

        private string GenerateCacheKey(string operation, string identifier)
        {
            var fnv1a = new Infrastructure.Cache.Hashing.FNV1a64();
            fnv1a.Update(ProviderId);
            fnv1a.Update(operation);
            fnv1a.Update(identifier);
            return $"redis:{fnv1a.Digest():x16}";
        }
    }
}

