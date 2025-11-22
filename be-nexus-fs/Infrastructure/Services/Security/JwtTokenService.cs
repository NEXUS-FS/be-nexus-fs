using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Application.Common.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Application.Common.Settings;

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
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("role", user.Role),
                new Claim("email", user.Email),
                new Claim("provider", user.Provider)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        public bool ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            try
            {
                handler.ValidateToken(token, _validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(token, _validationParameters, out _);

                var userIdClaim = principal.Claims
                    .FirstOrDefault(c => c.Type == "userId");

                return userIdClaim?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
