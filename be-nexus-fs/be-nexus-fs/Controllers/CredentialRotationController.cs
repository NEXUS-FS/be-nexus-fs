using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using Domain.Repositories;

namespace be_nexus_fs.Controllers
{
    /// <summary>
    /// Controller for managing credential rotation for storage providers.
    /// </summary>
    [ApiController]
    [Route("api/credentials")]
    [Produces("application/json")]
    public class CredentialRotationController : ControllerBase
    {
        private readonly IProviderRepository _providerRepository;
        private readonly ProviderManager _providerManager;
        private readonly ILogger<CredentialRotationController> _logger;

        public CredentialRotationController(
            IProviderRepository providerRepository,
            ProviderManager providerManager,
            ILogger<CredentialRotationController> logger)
        {
            _providerRepository = providerRepository;
            _providerManager = providerManager;
            _logger = logger;
        }

        /// <summary>
        /// Rotate credentials for a specific provider.
        /// </summary>
        [HttpPost("{providerId}/rotate")]
        [ProducesResponseType(typeof(RotateCredentialsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RotateCredentialsResponse>> RotateCredentials(
            string providerId,
            [FromBody] RotateCredentialsRequest request)
        {
            try
            {
                _logger.LogInformation("Rotating credentials for provider: {ProviderId}", providerId);

                // Get existing provider
                var providerEntity = await _providerRepository.GetByIdAsync(providerId);
                if (providerEntity == null)
                {
                    return NotFound(new RotateCredentialsResponse
                    {
                        Success = false,
                        Message = $"Provider '{providerId}' not found"
                    });
                }

                // Parse existing configuration
                var configuration = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                    providerEntity.Configuration ?? "{}") ?? new Dictionary<string, string>();

                // Update credentials based on provider type
                foreach (var credential in request.NewCredentials)
                {
                    configuration[credential.Key] = credential.Value;
                }

                // Test new credentials
                var testProvider = await _providerManager.CreateAndTestProvider(
                    providerEntity.Type,
                    providerId,
                    configuration);

                if (!testProvider.Success)
                {
                    return BadRequest(new RotateCredentialsResponse
                    {
                        Success = false,
                        Message = $"New credentials failed validation: {testProvider.Message}"
                    });
                }

                // Update provider configuration in database
                providerEntity.Configuration = System.Text.Json.JsonSerializer.Serialize(configuration);
                providerEntity.UpdatedAt = DateTime.UtcNow;
                await _providerRepository.UpdateAsync(providerEntity);

                // Reload provider in memory with new credentials
                await _providerManager.ReloadProvider(providerId);

                _logger.LogInformation("Successfully rotated credentials for provider: {ProviderId}", providerId);

                return Ok(new RotateCredentialsResponse
                {
                    Success = true,
                    Message = "Credentials rotated successfully",
                    ProviderId = providerId,
                    RotatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating credentials for provider: {ProviderId}", providerId);
                return StatusCode(500, new RotateCredentialsResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Test credentials for a provider without rotating.
        /// </summary>
        [HttpPost("{providerId}/test")]
        [ProducesResponseType(typeof(TestCredentialsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TestCredentialsResponse>> TestCredentials(
            string providerId,
            [FromBody] TestCredentialsRequest request)
        {
            try
            {
                _logger.LogInformation("Testing credentials for provider: {ProviderId}", providerId);

                var providerEntity = await _providerRepository.GetByIdAsync(providerId);
                if (providerEntity == null)
                {
                    return NotFound(new TestCredentialsResponse
                    {
                        Success = false,
                        Message = $"Provider '{providerId}' not found"
                    });
                }

                // Test credentials
                var testResult = await _providerManager.CreateAndTestProvider(
                    providerEntity.Type,
                    providerId,
                    request.Credentials);

                return Ok(new TestCredentialsResponse
                {
                    Success = testResult.Success,
                    Message = testResult.Message,
                    ProviderId = providerId,
                    TestedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing credentials for provider: {ProviderId}", providerId);
                return StatusCode(500, new TestCredentialsResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get credential rotation history for a provider.
        /// </summary>
        [HttpGet("{providerId}/history")]
        [ProducesResponseType(typeof(CredentialHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CredentialHistoryResponse>> GetRotationHistory(string providerId)
        {
            try
            {
                var providerEntity = await _providerRepository.GetByIdAsync(providerId);
                if (providerEntity == null)
                {
                    return NotFound(new CredentialHistoryResponse
                    {
                        Success = false,
                        Message = $"Provider '{providerId}' not found"
                    });
                }

                // In a real implementation, this would query an audit log table
                // For now, return basic info from the provider entity
                return Ok(new CredentialHistoryResponse
                {
                    Success = true,
                    ProviderId = providerId,
                    LastUpdated = providerEntity.UpdatedAt,
                    History = new List<CredentialRotationRecord>
                    {
                        new CredentialRotationRecord
                        {
                            RotatedAt = providerEntity.UpdatedAt ?? providerEntity.CreatedAt,
                            RotatedBy = "system",
                            Success = true
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rotation history for provider: {ProviderId}", providerId);
                return StatusCode(500, new CredentialHistoryResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }
    }

    // DTOs
    public class RotateCredentialsRequest
    {
        public Dictionary<string, string> NewCredentials { get; set; } = new();
    }

    public class RotateCredentialsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public DateTime? RotatedAt { get; set; }
    }

    public class TestCredentialsRequest
    {
        public Dictionary<string, string> Credentials { get; set; } = new();
    }

    public class TestCredentialsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public DateTime? TestedAt { get; set; }
    }

    public class CredentialHistoryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<CredentialRotationRecord> History { get; set; } = new();
    }

    public class CredentialRotationRecord
    {
        public DateTime RotatedAt { get; set; }
        public string RotatedBy { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Notes { get; set; }
    }
}

