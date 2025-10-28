namespace Infrastructure.Services
{
    /// <summary>
    /// Abstract base class for all storage provider implementations.
    /// AOP Concerns: Logging, Retry Logic, Metrics, Error Handling
    /// </summary>
    public abstract class Provider
    {
        public string ProviderId { get; protected set; }
        public string ProviderType { get; protected set; }
        public Dictionary<string, string> Configuration { get; protected set; }

        public Provider(string providerId, string providerType, Dictionary<string, string> configuration)
        {
            ProviderId = providerId;
            ProviderType = providerType;
            Configuration = configuration;
        }

        /// <summary>
        /// Reads a file from the storage provider.
        /// </summary>
        /// <param name="filePath">The file path to read</param>
        /// <returns>File content as string</returns>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit with result
        // [AOP: RetryAspect] AROUND: Retry on transient network/storage failures
        // [AOP: MetricsAspect] AROUND: Measure execution time and count operations
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public abstract Task<string> ReadFileAsync(string filePath);

        /// <summary>
        /// Writes content to a file in the storage provider.
        /// </summary>
        /// <param name="filePath">The file path to write</param>
        /// <param name="content">The content to write</param>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit
        // [AOP: RetryAspect] AROUND: Retry on transient network/storage failures
        // [AOP: MetricsAspect] AROUND: Measure execution time and count operations
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public abstract Task WriteFileAsync(string filePath, string content);

        /// <summary>
        /// Deletes a file from the storage provider.
        /// </summary>
        /// <param name="filePath">The file path to delete</param>
        // [AOP: LoggingAspect] BEFORE: Log method entry with parameters
        // [AOP: LoggingAspect] AFTER: Log method exit
        // [AOP: RetryAspect] AROUND: Retry on transient network/storage failures
        // [AOP: MetricsAspect] AROUND: Measure execution time and count operations
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public abstract Task DeleteFileAsync(string filePath);

        /// <summary>
        /// Tests the connection to the storage provider.
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        // [AOP: LoggingAspect] BEFORE: Log connection test attempt
        // [AOP: LoggingAspect] AFTER: Log connection test result
        // [AOP: RetryAspect] AROUND: Retry on transient connection failures
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public abstract Task<bool> TestConnectionAsync();

        /// <summary>
        /// Initializes the provider with the given configuration.
        /// </summary>
        /// <param name="config">Configuration dictionary</param>
        // [AOP: LoggingAspect] BEFORE: Log initialization attempt
        // [AOP: LoggingAspect] AFTER: Log initialization result
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public abstract Task Initialize(Dictionary<string, string> config);
		public abstract Task<List<string>> ListFilesAsync(string directoryPath, bool recursive);

    }
}