using Application.DTOs;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;

namespace Infrastructure.Services
{
    public class ProviderRouter
    {
        private readonly ProviderManager _providerManager;
        private readonly Logger _logger;
        private readonly AuthManager _authManager;

        public ProviderRouter(ProviderManager providerManager, Logger logger, AuthManager authManager)
        {
            _providerManager = providerManager;
            _logger = logger;
            _authManager = authManager;
        }

        public async Task<Provider> RouteToProvider(string providerId)
        {
            throw new NotImplementedException();
        }

        public async Task<FileOperationResponse> ExecuteOperation(
            string providerId, string operation, Dictionary<string, object> parameters)
        { 
            throw new NotImplementedException(); 
        }
    }
}
