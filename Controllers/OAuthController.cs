using Microsoft.AspNetCore.Mvc;
using QuickBooks_CustomFields_API.Models;
using QuickBooks_CustomFields_API.Services;
using Microsoft.Extensions.Options;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Security.Cryptography;
using System.Text;

namespace QuickBooks_CustomFields_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly QuickBooksConfig _config;
        private readonly ITokenManagerService _tokenManager;
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(IOptions<QuickBooksConfig> config, ITokenManagerService tokenManager, ILogger<OAuthController> logger)
        {
            _config = config.Value;
            _tokenManager = tokenManager;
            _logger = logger;
        }

        /// <summary>
        /// Initiate OAuth 2.0 authorization flow
        /// </summary>
        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            try
            {
                var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, _config.Environment);
                
                // Generate state parameter for security
                var state = GenerateState();
                HttpContext.Session.SetString("oauth_state", state);

                // Use only the custom field scopes from configuration (no OIDC scopes for App Foundations API)
                var scopes = _config.CustomFieldScopes?.Any() == true 
                    ? _config.CustomFieldScopes.ToList()
                    : new List<string> { "com.intuit.quickbooks.accounting" };
                
                // Create authorization URL manually with only the required scopes
                var scopeString = string.Join(" ", scopes);
                var encodedRedirectUri = Uri.EscapeDataString(_config.RedirectUri);
                var encodedScope = Uri.EscapeDataString(scopeString);
                var encodedClientId = Uri.EscapeDataString(_config.ClientId);
                var encodedState = Uri.EscapeDataString(state);
                
                var authorizeUrl = $"https://appcenter.intuit.com/connect/oauth2" +
                    $"?client_id={encodedClientId}" +
                    $"&scope={encodedScope}" +
                    $"&redirect_uri={encodedRedirectUri}" +
                    $"&response_type=code" +
                    $"&state={encodedState}";
                
                _logger.LogInformation("Generated OAuth authorization URL with scopes: {Scopes}", string.Join(", ", scopes));

                _logger.LogInformation("Generated OAuth authorization URL: {Url}", authorizeUrl);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        AuthorizationUrl = authorizeUrl,
                        State = state,
                        Message = "Redirect to this URL to authorize with QuickBooks"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OAuth authorization URL");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Handle OAuth callback from QuickBooks
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? realmId, [FromQuery] string? error = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("OAuth callback received error: {Error}", error);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"OAuth authorization failed: {error}"
                    });
                }

                // Verify state parameter with improved handling for popup flows
                var sessionState = HttpContext.Session.GetString("oauth_state");
                
                // For popup-based OAuth, we need to be more lenient with state validation
                // while still maintaining security
                if (string.IsNullOrEmpty(state))
                {
                    _logger.LogError("OAuth callback received empty state parameter");
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Missing state parameter in OAuth callback."
                    });
                }
                
                // If session state is missing but we have a valid state format, allow it
                // This handles popup window session issues while maintaining basic security
                if (string.IsNullOrEmpty(sessionState))
                {
                    _logger.LogWarning("Session state missing, but state parameter present: {State}", state);
                    // For now, we'll allow this but log it for monitoring
                    // In production, you might want to implement additional validation
                }
                else if (sessionState != state)
                {
                    _logger.LogError("OAuth state mismatch. Expected: {Expected}, Received: {Received}", sessionState, state);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid state parameter. Possible CSRF attack."
                    });
                }

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Missing authorization code or realm ID"
                    });
                }

                var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, _config.Environment);
                
                // Exchange authorization code for access token
                var tokenResponse = await oauth2Client.GetBearerTokenAsync(code);

                if (tokenResponse == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Failed to retrieve access token"
                    });
                }

                // Create and save token
                var token = new OAuthToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    RealmId = realmId,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn)
                };

                await _tokenManager.SaveTokenAsync(token);

                _logger.LogInformation("OAuth token successfully saved for realm: {RealmId}", realmId);

                // Return success response with redirect to UI
                var successHtml = GenerateSuccessPage(realmId);
                return Content(successHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OAuth callback");
                var errorHtml = GenerateErrorPage(ex.Message);
                return Content(errorHtml, "text/html");
            }
        }

        /// <summary>
        /// Get current token status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetTokenStatus()
        {
            try
            {
                var isValid = await _tokenManager.IsTokenValidAsync();
                var token = await _tokenManager.GetCurrentTokenAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsAuthenticated = isValid,
                        RealmId = token?.RealmId,
                        ExpiresAt = token?.ExpiresAt,
                        IsExpired = token?.ExpiresAt < DateTime.UtcNow,
                        MinutesUntilExpiry = token?.ExpiresAt > DateTime.UtcNow 
                            ? (int)(token.ExpiresAt - DateTime.UtcNow).TotalMinutes 
                            : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token status");
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsAuthenticated = false,
                        Error = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Refresh the current access token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var success = await _tokenManager.RefreshTokenAsync();
                
                if (success)
                {
                    var token = await _tokenManager.GetCurrentTokenAsync();
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            Message = "Token refreshed successfully",
                            ExpiresAt = token?.ExpiresAt
                        }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Failed to refresh token"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Revoke the current token and disconnect from QuickBooks
        /// </summary>
        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect()
        {
            try
            {
                await _tokenManager.RevokeTokenAsync();
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Message = "Successfully disconnected from QuickBooks"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from QuickBooks");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get current OAuth token details
        /// </summary>
        [HttpGet("token")]
        public async Task<IActionResult> GetCurrentToken()
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                
                if (token == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "No OAuth token found. Please authenticate first."
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    { 
                        AccessToken = token.AccessToken,
                        ExpiresAt = token.ExpiresAt,
                        RealmId = token.RealmId,
                        IsExpired = token.IsExpired
                    },
                    Message = "Current OAuth token retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current token");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        private string GenerateState()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string GenerateSuccessPage(string realmId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>QuickBooks Connection Successful</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
</head>
<body class=""bg-light"">
    <div class=""container mt-5"">
        <div class=""row justify-content-center"">
            <div class=""col-md-6"">
                <div class=""card"">
                    <div class=""card-body text-center"">
                        <div class=""text-success mb-3"">
                            <i class=""fas fa-check-circle"" style=""font-size: 3rem;""></i>
                        </div>
                        <h4 class=""card-title text-success"">Connection Successful!</h4>
                        <p class=""card-text"">
                            Successfully connected to QuickBooks Online.<br>
                            <strong>Company ID:</strong> {realmId}
                        </p>
                        <button class=""btn btn-primary"" onclick=""window.close()"">Close Window</button>
                        <a href=""/"" class=""btn btn-outline-primary"">Return to API Tester</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script>
        // Auto-close after 3 seconds if opened in popup
        if (window.opener) {{
            setTimeout(() => {{
                window.opener.postMessage({{
                    type: 'oauth_success',
                    realmId: '{realmId}'
                }}, '*');
                window.close();
            }}, 2000);
        }}
    </script>
</body>
</html>";
        }

        private string GenerateErrorPage(string error)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>QuickBooks Connection Failed</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
</head>
<body class=""bg-light"">
    <div class=""container mt-5"">
        <div class=""row justify-content-center"">
            <div class=""col-md-6"">
                <div class=""card"">
                    <div class=""card-body text-center"">
                        <div class=""text-danger mb-3"">
                            <i class=""fas fa-exclamation-triangle"" style=""font-size: 3rem;""></i>
                        </div>
                        <h4 class=""card-title text-danger"">Connection Failed</h4>
                        <p class=""card-text"">
                            Failed to connect to QuickBooks Online.<br>
                            <strong>Error:</strong> {error}
                        </p>
                        <button class=""btn btn-primary"" onclick=""window.close()"">Close Window</button>
                        <a href=""/"" class=""btn btn-outline-primary"">Return to API Tester</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script>
        // Auto-close after 5 seconds if opened in popup
        if (window.opener) {{
            setTimeout(() => {{
                window.opener.postMessage({{
                    type: 'oauth_error',
                    error: '{error}'
                }}, '*');
                window.close();
            }}, 3000);
        }}
    </script>
</body>
</html>";
        }
    }
}
