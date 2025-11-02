namespace Infrastructure.Configuration
{
    /// <summary>
    /// Configuration options for Clerk authentication
    /// </summary>
    public class ClerkOptions
    {
        public const string SectionName = "Clerk";
        
        /// <summary>
        /// Clerk API secret key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Clerk API version
        /// </summary>
        public string ApiVersion { get; set; } = "v1";
        
        /// <summary>
        /// Clerk API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.clerk.dev";
        
        /// <summary>
        /// JWKS endpoint URL for JWT token validation
        /// </summary>
        public string JwksUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// JWT token issuer
        /// </summary>
        public string Issuer { get; set; } = string.Empty;
        
        /// <summary>
        /// JWT token audience
        /// </summary>
        public string Audience { get; set; } = string.Empty;
    }
}