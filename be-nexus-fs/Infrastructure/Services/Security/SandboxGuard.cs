/// <summary>
/// Proxy Pattern implementation.
/// Enforces sandbox restrictions and validates resource access.
/// </summary>

namespace Infrastructure.Services.Security
{
    public class SandboxGuard
    {
        Task<bool> EnforceAsync(Dictionary<string, string> context)
        {
            throw new NotImplementedException();
        }
        Task<bool> ValidateResourceAsync(string resourceName)
        {
            throw new NotImplementedException();
        }
        
        Task<bool> RestrictOperationAsync(string operationName)
        {
            throw new NotImplementedException();
        }
        
        string StrategyName { get; } //we need to specify different strategies for different sandboxes
    }
}
