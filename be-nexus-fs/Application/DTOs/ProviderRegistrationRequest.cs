namespace Application.DTOs
{
    public class ProviderRegistrationRequest
    {
        /// <summary>
        /// Your internal identifier for this provider (used in file ops requests).
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Provider type. Use "GoogleDrive" (aliases: google, gdrive, drive).
        /// </summary>
        public string ProviderType { get; set; } = string.Empty;

        /// <summary>
        /// Arbitrary key/value settings. For Google Drive:
        /// clientId (required) - OAuth client ID
        /// clientSecret (required) - OAuth client secret
        /// refreshToken (required) - long-lived token to obtain access tokens
        /// accessToken (optional) - initial access token (will refresh)
        /// rootFolderId (optional) - "root" or a specific folder ID to sandbox
        /// applicationName (optional) - defaults to NexusFS
        /// pathCacheTtlSeconds (optional) - cache TTL for path-to-ID mapping
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; } = new();
    }
}
