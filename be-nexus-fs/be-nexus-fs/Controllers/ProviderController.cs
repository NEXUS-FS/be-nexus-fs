using Application.DTOs;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
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

        /// <summary>
        /// Register a Google Drive provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "gdrive-demo",
        ///   "configuration": {
        ///     "clientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
        ///     "clientSecret": "YOUR_CLIENT_SECRET",
        ///     "refreshToken": "YOUR_REFRESH_TOKEN"
        ///   }
        /// }
        /// Required fields in configuration: clientId, clientSecret, refreshToken.
        /// </remarks>
        [Authorize]
        [HttpPost("google")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

            const string providerType = "GoogleDrive";

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

        /// <summary>
        /// Register an S3 provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "s3-demo",
        ///   "configuration": {
        ///     "AccessKey": "YOUR_ACCESS_KEY",
        ///     "SecretKey": "YOUR_SECRET_KEY",
        ///     "Bucket": "your-bucket-name",
        ///     "Region": "us-east-1"
        ///   }
        /// }
        /// Required fields in configuration: AccessKey, SecretKey, Bucket, Region.
        /// </remarks>
        [Authorize]
        [HttpPost("s3")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterS3Provider([FromBody] ProviderRegistrationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest("ProviderId is required.");
            }

            const string providerType = "S3";

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
                _logger.LogError(ex, "Failed to register S3 provider");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to register provider: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a Local filesystem provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "local-demo",
        ///   "configuration": {
        ///     "RootPath": "./nexus_storage"
        ///   }
        /// }
        /// Required fields in configuration: RootPath.
        /// </remarks>
        [Authorize]
        [HttpPost("local")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterLocalProvider([FromBody] ProviderRegistrationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest("ProviderId is required.");
            }

            const string providerType = "Local";

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
                _logger.LogError(ex, "Failed to register Local provider");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to register provider: {ex.Message}");
            }
        }

        /// <summary>
        /// Register an FTP provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "ftp-demo",
        ///   "configuration": {
        ///     "Host": "ftp.example.com",
        ///     "Username": "your-username",
        ///     "Password": "your-password"
        ///   }
        /// }
        /// Required fields in configuration: Host, Username, Password.
        /// </remarks>
        [Authorize]
        [HttpPost("ftp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterFtpProvider([FromBody] ProviderRegistrationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest("ProviderId is required.");
            }

            const string providerType = "Ftp";

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
                _logger.LogError(ex, "Failed to register FTP provider");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to register provider: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a Memory provider.
        /// </summary>
        /// <remarks>
        /// Example payload:
        /// {
        ///   "providerId": "memory-demo",
        ///   "configuration": {}
        /// }
        /// Configuration can be empty for Memory provider. No required fields.
        /// </remarks>
        [Authorize]
        [HttpPost("memory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterMemoryProvider([FromBody] ProviderRegistrationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ProviderId))
            {
                return BadRequest("ProviderId is required.");
            }

            const string providerType = "Memory";

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
                _logger.LogError(ex, "Failed to register Memory provider");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to register provider: {ex.Message}");
            }
        }
    }
}
