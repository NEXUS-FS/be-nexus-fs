using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Common.Security
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generate a signed JWT access token containing claims (userId, username, role).
        /// </summary>
        string GenerateAccessToken(UserEntity user);

        /// <summary>
        /// Generate a secure refresh token.
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Validate the JWT's signature and expiration. Returns true if valid.
        /// </summary>
        bool ValidateToken(string token);

        /// <summary>
        /// Extract the user id from a valid JWT token. Should throw or return null/empty if invalid.
        /// </summary>
        string? GetUserIdFromToken(string token);
    }
}
