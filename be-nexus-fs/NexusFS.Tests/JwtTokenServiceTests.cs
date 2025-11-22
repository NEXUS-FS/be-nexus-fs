using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Settings;
using Domain.Entities;
using Infrastructure.Services.Security;
using Microsoft.Extensions.Options;

namespace NexusFS.Tests
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service;

        private readonly UserEntity _dummyUser = new UserEntity
        {
            Id = Guid.NewGuid().ToString(),
            Username = "testuser",
            Role = "Admin",
            Email = "test@example.com",
            Provider = "Local"
        };

        public JwtTokenServiceTests()
        {
            var settings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SecretKey = "THIS_IS_A_VERY_SECRET_KEY_1234567890",
                AccessTokenMinutes = 60,
                RefreshTokenDays = 7
            };

            _service = new JwtTokenService(
                Options.Create(settings)
            );
        }

        [Fact]
        public void GenerateAccessToken_ShouldReturn_NonEmptyToken()
        {
            var token = _service.GenerateAccessToken(_dummyUser);

            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void ValidateToken_ShouldReturnTrue_ForValidToken()
        {
            var token = _service.GenerateAccessToken(_dummyUser);

            var result = _service.ValidateToken(token);

            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForInvalidToken()
        {
            var result = _service.ValidateToken("test");

            Assert.False(result);
        }

        [Fact]
        public void GetUserIdFromToken_ShouldExtractUserId()
        {
            var token = _service.GenerateAccessToken(_dummyUser);

            var userId = _service.GetUserIdFromToken(token);

            Assert.Equal(_dummyUser.Id.ToString(), userId);
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnRandomSecureString()
        {
            var token1 = _service.GenerateRefreshToken();
            var token2 = _service.GenerateRefreshToken();

            Assert.False(string.IsNullOrWhiteSpace(token1));
            Assert.False(string.IsNullOrWhiteSpace(token2));
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_WhenTokenIsExpired()
        {
            // Arrange
            var settings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SecretKey = "THIS_IS_A_VERY_SECRET_KEY_1234567890",
                AccessTokenMinutes = 0,   // expires immediately
                RefreshTokenDays = 7
            };

            var service = new JwtTokenService(Options.Create(settings));

            var token = service.GenerateAccessToken(_dummyUser);

            // Wait a moment so token is definitely expired
            await Task.Delay(1000);

            // Act
            var result = service.ValidateToken(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_WhenTokenIsTampered()
        {
            var token = _service.GenerateAccessToken(_dummyUser);

            var parts = token.Split('.');
            parts[1] = parts[1].Replace(parts[1][0], 'A');

            var tampered = string.Join(".", parts);

            var result = _service.ValidateToken(tampered);

            Assert.False(result);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_WhenAudienceDoesNotMatch()
        {
            // Create a service with different audience
            var wrongSettings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "WrongAudience",
                SecretKey = "THIS_IS_A_VERY_SECRET_KEY_1234567890",
                AccessTokenMinutes = 60,
                RefreshTokenDays = 7
            };

            var wrongService = new JwtTokenService(Options.Create(wrongSettings));

            var token = wrongService.GenerateAccessToken(_dummyUser);

            // This service expects TestAudience
            var result = _service.ValidateToken(token);

            Assert.False(result);
        }

        [Fact]
        public void GenerateAccessToken_ShouldContainCorrectClaims()
        {
            var token = _service.GenerateAccessToken(_dummyUser);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal(_dummyUser.Id.ToString(), jwt.Claims.First(c => c.Type == "userId").Value);
            Assert.Equal(_dummyUser.Username, jwt.Claims.First(c => c.Type == "username").Value);
            Assert.Equal(_dummyUser.Role, jwt.Claims.First(c => c.Type == "role").Value);
        }
    }
}
