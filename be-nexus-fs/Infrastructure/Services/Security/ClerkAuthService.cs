using Infrastructure.Configuration;
using Infrastructure.Services.Security;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Clerk.Net;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Service for handling Clerk authentication operations
    /// </summary>
    public interface IClerkAuthService
    {
        Task<bool> ValidateTokenAsync(string token);
        ClaimsPrincipal? GetUserClaims(string token);
        string? GetUserId(string token);
        string? GetUserEmail(string token);
        string? ExtractTokenFromRequest(HttpRequest request);
    }

    public class ClerkAuthService : IClerkAuthService
    {
        private readonly ClerkAuthStrategy _clerkStrategy;

        public ClerkAuthService(ClerkAuthStrategy clerkStrategy)
        {
            _clerkStrategy = clerkStrategy;
        }

        /// <summary>
        /// Validates a Clerk JWT token
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            return await _clerkStrategy.ValidateTokenAsync(token);
        }

        /// <summary>
        /// Extracts user claims from token
        /// </summary>
        public ClaimsPrincipal? GetUserClaims(string token)
        {
            return _clerkStrategy.GetUserClaims(token);
        }

        /// <summary>
        /// Gets user ID from token
        /// </summary>
        public string? GetUserId(string token)
        {
            return _clerkStrategy.GetUserId(token);
        }

        /// <summary>
        /// Gets user email from token claims
        /// </summary>
        public string? GetUserEmail(string token)
        {
            var claims = GetUserClaims(token);
            return claims?.FindFirst("email")?.Value ?? 
                   claims?.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Extracts JWT token from HTTP request Authorization header
        /// </summary>
        public string? ExtractTokenFromRequest(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader))
                return null;

            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }

            return null;
        }
    }
}