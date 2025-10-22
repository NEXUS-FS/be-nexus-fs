using Application.DTOs;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;

namespace Infrastructure.Services
{
    public class NexusApi
    {
        private readonly ProviderRouter _providerRouter;
        private readonly MCPServerProxy _mcpServerProxy;
        private readonly Logger _logger;

        public NexusApi(ProviderRouter providerRouter, MCPServerProxy mcpServerProxy, Logger logger)
        {
            _providerRouter = providerRouter;
            _mcpServerProxy = mcpServerProxy;
            _logger = logger;
        }

        public async Task<FileOperationResponse> ReadFile(string providerId, string filePath)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task<FileOperationResponse> WriteFile(string providerId, string filePath, string content)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public async Task<FileOperationResponse> DeleteFile(string providerId, string filePath)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }
    }
}
