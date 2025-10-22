/// <summary>
/// Strategy Pattern interface for authentication methods.
/// Defines the contract for all authentication strategies.
/// </summary>

namespace Infrastructure.Services.Security
{
    public interface IAuthStrategy
    {
        //Task<bool> AuthenticateAsync(Dictionary<string, string> credentials);
        //Task<bool> ValidateTokenAsync(string token);
        //Task<string> GenerateTokenAsync(Dictionary<string, string> credentials);
        //Task RevokeTokenAsync(string token);
        //string StrategyName { get; }
    }
}
