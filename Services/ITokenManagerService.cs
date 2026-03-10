using QuickBooks_CustomFields_API.Models;

namespace QuickBooks_CustomFields_API.Services
{
    public interface ITokenManagerService
    {
        Task<OAuthToken?> GetCurrentTokenAsync();
        Task SaveTokenAsync(OAuthToken token);
        Task<bool> RefreshTokenAsync();
        Task<bool> IsTokenValidAsync();
        Task RevokeTokenAsync();
    }
}
