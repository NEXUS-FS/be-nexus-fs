using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Application.Common.Security;

namespace Infrastructure.Services.Security
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;
        private readonly int _accessTokenMinutes;
        private readonly int _refreshTokenDays;

        private readonly TokenValidationParameters _validationParameters;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            var settings = options.Value;

            _issuer = settings.Issuer;
            _audience = settings.Audience;
            _secretKey = settings.SecretKey;
            _accessTokenMinutes = settings.AccessTokenMinutes;
            _refreshTokenDays = settings.RefreshTokenDays;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,

                ValidateAudience = true,
                ValidAudience = _audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key
            };
        }

        public string GenerateAccessToken(UserEntity user)
            => throw new NotImplementedException();

        public string GenerateRefreshToken()
            => throw new NotImplementedException();

        public bool ValidateToken(string token)
            => throw new NotImplementedException();

        public string? GetUserIdFromToken(string token)
            => throw new NotImplementedException();
    }
}
