using Application.DTOs;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace be_nexus_fs.Controllers
{
    [ApiController]
    [Route("api/providers")]
    public class ProviderController : ControllerBase
    {
        private readonly ProviderFactory _providerFactory;
        private readonly ProviderManager _providerManager;
        private readonly ILogger<ProviderController> _logger;

        public ProviderController(
            ProviderFactory providerFactory,
            ProviderManager providerManager,
            ILogger<ProviderController> logger)
        {
            _providerFactory = providerFactory;
            _providerManager = providerManager;
            _logger = logger;
        }

        [HttpPost("google")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        /// <summary>
        /// Register a Google Drive provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "gdrive-demo",
        ///   "providerType": "GoogleDrive",
        ///   "configuration": {
        ///     "clientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
        ///     "clientSecret": "YOUR_CLIENT_SECRET",
        ///     "refreshToken": "YOUR_REFRESH_TOKEN",
        ///     "rootFolderId": "root"
        ///   }
        /// }
        /// Required config keys: clientId, clientSecret, refreshToken.
        /// Optional: accessToken, rootFolderId, applicationName, pathCacheTtlSeconds.
        /// </remarks>
        public async Task<IActionResult> RegisterGoogleDriveProvider([FromBody] ProviderRegistrationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest("ProviderId is required.");
            }

            var providerType = string.IsNullOrWhiteSpace(request.ProviderType)
                ? "GoogleDrive"
                : request.ProviderType;

            if (!_providerFactory.IsProviderTypeSupported(providerType))
            {
                return BadRequest($"Provider type '{providerType}' is not supported.");
            }

            try
            {
                var provider = await _providerFactory.CreateProviderAsync(providerType, request.ProviderId, request.Configuration);
                await _providerManager.RegisterProvider(provider);

                return Ok(new
                {
                    providerId = provider.ProviderId,
                    providerType = provider.ProviderType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register Google Drive provider");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to register provider: {ex.Message}");
            }
        }
    }
}
