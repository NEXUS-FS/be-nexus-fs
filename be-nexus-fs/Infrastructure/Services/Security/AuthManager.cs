using Infrastructure.Services.Observability;

namespace Infrastructure.Services.Security
{
    /// <summary>
    /// Strategy Pattern context.
    /// Central authentication handler that applies the selected authentication strategy.
    /// AOP Concerns: Logging, Metrics, Error Handling
    /// </summary>
    public class AuthManager
    {
        private readonly Dictionary<string, IAuthStrategy> _strategies;
        private readonly Logger _logger;
        private IAuthStrategy? _currentStrategy;

        public AuthManager(Logger logger)
        {
            _strategies = new Dictionary<string, IAuthStrategy>();
            _logger = logger;
            InitializeStrategies();
        }

        /// <summary>
        /// Initializes available authentication strategies.
        /// </summary>
        // [AOP: LoggingAspect] BEFORE: Log initialization start
        // [AOP: LoggingAspect] AFTER: Log strategies registered
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        private void InitializeStrategies()
        {
            try
            {
                _logger.LogInformation("Initializing authentication strategies", "AuthManager");

                // Register Basic Authentication Strategy
                var basicStrategy = new BasicAuthStrategy();
                RegisterStrategy(basicStrategy);

                // Register Google OAuth Strategy
                var googleOAuthStrategy = new GoogleOAuthStrategy();
                RegisterStrategy(googleOAuthStrategy);

                // Set Basic as default strategy
                _currentStrategy = basicStrategy;

                _logger.LogInformation($"Registered {_strategies.Count} authentication strategies", "AuthManager");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing authentication strategies: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the active authentication strategy.
        /// </summary>
        /// <param name="strategyName">The name of the strategy to activate</param>
        // [AOP: LoggingAspect] BEFORE: Log strategy change attempt
        // [AOP: LoggingAspect] AFTER: Log strategy changed
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public void SetStrategy(string strategyName)
        {
            try
            {
                _logger.LogInformation($"Attempting to set strategy to: {strategyName}", "AuthManager");

                if (string.IsNullOrWhiteSpace(strategyName))
                {
                    throw new ArgumentException("Strategy name cannot be null or empty", nameof(strategyName));
                }

                if (!_strategies.ContainsKey(strategyName))
                {
                    var availableStrategies = string.Join(", ", _strategies.Keys);
                    throw new InvalidOperationException(
                        $"Strategy '{strategyName}' not found. Available strategies: {availableStrategies}");
                }

                _currentStrategy = _strategies[strategyName];
                _logger.LogInformation($"Successfully set strategy to: {strategyName}", "AuthManager");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error setting strategy to '{strategyName}': {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Selects and sets the authentication strategy based on request headers or parameters.
        /// Looks for 'X-Auth-Strategy', 'Authentication-Type', or 'AuthType' headers/parameters.
        /// Defaults to 'Basic' if no strategy is specified.
        /// </summary>
        /// <param name="requestData">Dictionary containing headers or parameters from the request</param>
        // [AOP: LoggingAspect] BEFORE: Log strategy selection attempt
        // [AOP: LoggingAspect] AFTER: Log strategy selected
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public void SelectStrategyFromRequest(Dictionary<string, string> requestData)
        {
            try
            {
                _logger.LogInformation("Selecting authentication strategy from request", "AuthManager");

                if (requestData == null || requestData.Count == 0)
                {
                    _logger.LogInformation("No request data provided, using default strategy: Basic", "AuthManager");
                    SetStrategy("Basic");
                    return;
                }

                // Check for strategy specification in common headers/parameters
                string? strategyName = null;
                
                // Try different possible header/parameter names (case-insensitive)
                var possibleKeys = new[] { "X-Auth-Strategy", "Authentication-Type", "AuthType", "auth-strategy", "auth_type" };
                
                foreach (var key in possibleKeys)
                {
                    var matchingKey = requestData.Keys.FirstOrDefault(k => 
                        k.Equals(key, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingKey != null)
                    {
                        strategyName = requestData[matchingKey];
                        _logger.LogInformation($"Found strategy specification in '{matchingKey}': {strategyName}", "AuthManager");
                        break;
                    }
                }

                // If no explicit strategy specified, try to detect from credentials
                if (string.IsNullOrWhiteSpace(strategyName))
                {
                    strategyName = DetectStrategyFromCredentials(requestData);
                }

                // Default to Basic if still not determined
                if (string.IsNullOrWhiteSpace(strategyName))
                {
                    _logger.LogInformation("No strategy specified, defaulting to: Basic", "AuthManager");
                    strategyName = "Basic";
                }

                // Set the determined strategy
                SetStrategy(strategyName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error selecting strategy from request: {ex.Message}", "AuthManager", ex);
                // Default to Basic strategy on error
                _logger.LogWarning("Falling back to default strategy: Basic", "AuthManager");
                SetStrategy("Basic");
            }
        }

        /// <summary>
        /// Detects the authentication strategy based on credential patterns.
        /// </summary>
        /// <param name="credentials">The credentials dictionary</param>
        /// <returns>The detected strategy name, or null if cannot determine</returns>
        private string? DetectStrategyFromCredentials(Dictionary<string, string> credentials)
        {
            // Check for OAuth-specific fields
            if (credentials.ContainsKey("access_token") || 
                credentials.ContainsKey("id_token") ||
                credentials.ContainsKey("refresh_token") ||
                credentials.ContainsKey("code"))
            {
                _logger.LogDebug("Detected OAuth credentials pattern", "AuthManager");
                return "OAuth";
            }

            // Check for Basic auth pattern (username/password)
            if (credentials.ContainsKey("username") && credentials.ContainsKey("password"))
            {
                _logger.LogDebug("Detected Basic auth credentials pattern", "AuthManager");
                return "Basic";
            }

            // Check for Authorization header patterns
            if (credentials.ContainsKey("Authorization"))
            {
                var authHeader = credentials["Authorization"];
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Detected Bearer token (OAuth) pattern", "AuthManager");
                    return "OAuth";
                }
                else if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Detected Basic auth header pattern", "AuthManager");
                    return "Basic";
                }
            }

            return null;
        }

        /// <summary>
        /// Registers a new authentication strategy.
        /// </summary>
        /// <param name="strategy">The strategy to register</param>
        // [AOP: LoggingAspect] BEFORE: Log strategy registration
        // [AOP: LoggingAspect] AFTER: Log registration success
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public void RegisterStrategy(IAuthStrategy strategy)
        {
            try
            {
                if (strategy == null)
                {
                    throw new ArgumentNullException(nameof(strategy), "Strategy cannot be null");
                }

                _logger.LogInformation($"Registering strategy: {strategy.StrategyName}", "AuthManager");

                if (_strategies.ContainsKey(strategy.StrategyName))
                {
                    _logger.LogWarning($"Strategy '{strategy.StrategyName}' already registered. Overwriting.", "AuthManager");
                }

                _strategies[strategy.StrategyName] = strategy;
                _logger.LogInformation($"Successfully registered strategy: {strategy.StrategyName}", "AuthManager");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering strategy: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Authenticates a user using the current strategy.
        /// </summary>
        /// <param name="credentials">User credentials</param>
        /// <returns>True if authentication successful, false otherwise</returns>
        // [AOP: LoggingAspect] BEFORE: Log authentication attempt (without sensitive data)
        // [AOP: LoggingAspect] AFTER: Log authentication result
        // [AOP: MetricsAspect] AROUND: Count authentication attempts and measure time
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public async Task<bool> AuthenticateAsync(Dictionary<string, string> credentials)
        {
            try
            {
                if (_currentStrategy == null)
                {
                    throw new InvalidOperationException("No authentication strategy is currently set");
                }

                _logger.LogInformation($"Authenticating using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                var result = await _currentStrategy.AuthenticateAsync(credentials);

                _logger.LogInformation($"Authentication {(result ? "successful" : "failed")} using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during authentication: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Authenticates a user by automatically selecting the appropriate strategy based on request data.
        /// </summary>
        /// <param name="credentials">User credentials</param>
        /// <param name="requestData">Request headers or parameters to determine authentication strategy</param>
        /// <returns>True if authentication successful, false otherwise</returns>
        // [AOP: LoggingAspect] BEFORE: Log authentication attempt (without sensitive data)
        // [AOP: LoggingAspect] AFTER: Log authentication result
        // [AOP: MetricsAspect] AROUND: Count authentication attempts and measure time
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public async Task<bool> AuthenticateAsync(Dictionary<string, string> credentials, Dictionary<string, string> requestData)
        {
            try
            {
                _logger.LogInformation("Authenticating with automatic strategy selection", "AuthManager");

                // Select strategy based on request data
                SelectStrategyFromRequest(requestData);

                // Perform authentication with selected strategy
                return await AuthenticateAsync(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during authentication with auto strategy selection: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates an authentication token.
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <returns>True if token is valid, false otherwise</returns>
        // [AOP: LoggingAspect] BEFORE: Log token validation attempt
        // [AOP: LoggingAspect] AFTER: Log validation result
        // [AOP: MetricsAspect] AROUND: Count validation attempts
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                if (_currentStrategy == null)
                {
                    throw new InvalidOperationException("No authentication strategy is currently set");
                }

                _logger.LogInformation($"Validating token using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                var result = await _currentStrategy.ValidateTokenAsync(token);

                _logger.LogInformation($"Token validation {(result ? "successful" : "failed")} using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during token validation: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates an authentication token.
        /// </summary>
        /// <param name="credentials">User credentials</param>
        /// <returns>Generated token string</returns>
        // [AOP: LoggingAspect] BEFORE: Log token generation attempt
        // [AOP: LoggingAspect] AFTER: Log token generation success
        // [AOP: MetricsAspect] AROUND: Count token generations
        // [AOP: ErrorHandlingAspect] AFTER_THROWING: Capture and log exceptions
        public async Task<string> GenerateTokenAsync(Dictionary<string, string> credentials)
        {
            try
            {
                if (_currentStrategy == null)
                {
                    throw new InvalidOperationException("No authentication strategy is currently set");
                }

                _logger.LogInformation($"Generating token using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                var token = await _currentStrategy.GenerateTokenAsync(credentials);

                _logger.LogInformation($"Token generated successfully using strategy: {_currentStrategy.StrategyName}", "AuthManager");

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during token generation: {ex.Message}", "AuthManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the currently active authentication strategy.
        /// </summary>
        /// <returns>The current strategy instance</returns>
        // [AOP: LoggingAspect] BEFORE: Log strategy retrieval
        public IAuthStrategy GetCurrentStrategy()
        {
            _logger.LogDebug($"Getting current strategy: {_currentStrategy?.StrategyName ?? "None"}", "AuthManager");

            if (_currentStrategy == null)
            {
                throw new InvalidOperationException("No authentication strategy is currently set");
            }

            return _currentStrategy;
        }
    }
}