namespace Infrastructure.Services
{
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

        public abstract Task<string> ReadFileAsync(string filePath);
        public abstract Task WriteFileAsync(string filePath, string content);
        public abstract Task DeleteFileAsync(string filePath);
        public abstract Task<bool> TestConnectionAsync();
        public abstract Task Initialize(Dictionary<string, string> config);
    }
}
