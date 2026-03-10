using QuickBooks_CustomFields_API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Intuit.Ipp.OAuth2PlatformClient;

namespace QuickBooks_CustomFields_API.Services
{
    public class TokenManagerService : ITokenManagerService
    {
        private readonly QuickBooksConfig _config;
        private readonly string _tokenFilePath;
        private OAuthToken? _currentToken;

        public TokenManagerService(IOptions<QuickBooksConfig> config)
        {
            _config = config.Value;
            _tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "token.json");
        }

        public async Task<OAuthToken?> GetCurrentTokenAsync()
        {
            if (_currentToken == null)
            {
                await LoadTokenFromFileAsync();
            }

            if (_currentToken?.IsExpired == true)
            {
                var refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    _currentToken = null;
                }
            }

            return _currentToken;
        }

        public async Task SaveTokenAsync(OAuthToken token)
        {
            _currentToken = token;
            var json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_tokenFilePath, json);
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (_currentToken?.RefreshToken == null)
                return false;

            try
            {
                var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, "sandbox");
                
                var tokenResponse = await oauth2Client.RefreshTokenAsync(_currentToken.RefreshToken);
                
                if (tokenResponse != null)
                {
                    var newToken = new OAuthToken
                    {
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken ?? _currentToken.RefreshToken,
                        RealmId = _currentToken.RealmId,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn)
                    };

                    await SaveTokenAsync(newToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing token: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> IsTokenValidAsync()
        {
            var token = await GetCurrentTokenAsync();
            return token != null && !token.IsExpired;
        }

        public async Task RevokeTokenAsync()
        {
            if (_currentToken?.RefreshToken != null)
            {
                try
                {
                    var oauth2Client = new OAuth2Client(_config.ClientId, _config.ClientSecret, _config.RedirectUri, "sandbox");
                    await oauth2Client.RevokeTokenAsync(_currentToken.RefreshToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error revoking token: {ex.Message}");
                }
            }

            _currentToken = null;
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }

        private async Task LoadTokenFromFileAsync()
        {
            if (File.Exists(_tokenFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_tokenFilePath);
                    _currentToken = JsonSerializer.Deserialize<OAuthToken>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading token from file: {ex.Message}");
                    _currentToken = null;
                }
            }
        }
    }
}
