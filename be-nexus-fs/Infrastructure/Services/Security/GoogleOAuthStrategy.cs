/// <summary>
/// Strategy Pattern implementation for Google OAuth authentication.
/// </summary>

namespace Infrastructure.Services.Security
{
    public class GoogleOAuthStrategy : IAuthStrategy
    {
        public string StrategyName => "OAuth";

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
