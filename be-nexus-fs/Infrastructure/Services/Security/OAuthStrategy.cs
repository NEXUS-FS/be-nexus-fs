/// <summary>
/// Strategy Pattern implementation for OAuth2 authentication.
/// </summary>

namespace Infrastructure.Services.Security
{
    public class OAuthStrategy : IAuthStrategy
    {
        public string StrategyName => "OAuth2";

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
