


/// <summary>
/// Strategy Pattern implementation for Basic authentication.
/// </summary>
namespace Infrastructure.Services.Security
{
    public class BasicAuthStrategy : IAuthStrategy
    {
        public string StrategyName => "BasicAuth";

        public Task<bool> AuthenticateAsync(Dictionary<string, string> credentials)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateTokenAsync(Dictionary<string, string> credentials)
        {
            throw new NotImplementedException();
        }

        public Task RevokeTokenAsync(string token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            throw new NotImplementedException();
        }
    }
}
