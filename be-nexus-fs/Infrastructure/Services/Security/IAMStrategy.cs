


/// <summary>
/// Strategy Pattern implementation for AWS IAM authentication.
/// </summary>
namespace Infrastructure.Services.Security
{
    public class IamStrategy : IAuthStrategy
    {
        public string StrategyName => "IAM";

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
