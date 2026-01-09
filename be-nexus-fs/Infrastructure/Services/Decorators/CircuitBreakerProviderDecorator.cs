using Domain.Models;
using Infrastructure.Services.Observability;
using Polly;
using Polly.CircuitBreaker;

namespace Infrastructure.Services.Decorators
{
    /// <summary>
    /// Circuit Breaker decorator for providers using Polly.
    /// Prevents cascading failures by breaking the circuit after consecutive failures.
    /// </summary>
    public class CircuitBreakerProviderDecorator : Provider
    {
        private readonly Provider _decoratedProvider;
        private readonly Logger? _logger;
        private readonly ResiliencePipeline _resiliencePipeline;

        public CircuitBreakerProviderDecorator(
            Provider decoratedProvider,
            Logger? logger = null,
            int failureThreshold = 5,
            TimeSpan? breakDuration = null)
            : base(decoratedProvider.ProviderId, decoratedProvider.ProviderType, decoratedProvider.Configuration)
        {
            _decoratedProvider = decoratedProvider ?? throw new ArgumentNullException(nameof(decoratedProvider));
            _logger = logger;

            // Build resilience pipeline with circuit breaker
            _resiliencePipeline = new ResiliencePipelineBuilder()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5, // Break if 50% of requests fail
                    SamplingDuration = TimeSpan.FromSeconds(30), // Sample window
                    MinimumThroughput = failureThreshold, // Minimum requests before breaking
                    BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30), // How long to keep circuit open
                    OnOpened = args =>
                    {
                        _logger?.LogWarning($"Circuit breaker OPENED for provider {ProviderId}. Break duration: {args.BreakDuration}", "CircuitBreakerDecorator");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger?.LogInformation($"Circuit breaker CLOSED for provider {ProviderId}", "CircuitBreakerDecorator");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger?.LogInformation($"Circuit breaker HALF-OPEN for provider {ProviderId}", "CircuitBreakerDecorator");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        public Provider DecoratedProvider => _decoratedProvider;

        public override async Task Initialize(Dictionary<string, string> config)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.Initialize(config), CancellationToken.None);
        }

        public override async Task<string> ReadFileAsync(string filePath)
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.ReadFileAsync(filePath), CancellationToken.None);
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.WriteFileAsync(filePath, content), CancellationToken.None);
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.DeleteFileAsync(filePath), CancellationToken.None);
        }

        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.ListFilesAsync(directoryPath, recursive), CancellationToken.None);
        }

        public override async Task<FileMetadata> StatAsync(string path)
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.StatAsync(path), CancellationToken.None);
        }

        public override async Task MkdirAsync(string path, bool recursive)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.MkdirAsync(path, recursive), CancellationToken.None);
        }

        public override async Task CopyAsync(string source, string destination)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.CopyAsync(source, destination), CancellationToken.None);
        }

        public override async Task MoveAsync(string source, string destination)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.MoveAsync(source, destination), CancellationToken.None);
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.ExistsAsync(path), CancellationToken.None);
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.ReadStreamAsync(filePath), CancellationToken.None);
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.WriteStreamAsync(filePath, content), CancellationToken.None);
        }

        public override async Task<bool> TestConnectionAsync()
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => 
                await _decoratedProvider.TestConnectionAsync(), CancellationToken.None);
        }
    }
}

