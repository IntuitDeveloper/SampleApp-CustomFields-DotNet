using Microsoft.AspNetCore.Mvc;
using QuickBooks_CustomFields_API.Models;
using QuickBooks_CustomFields_API.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace QuickBooks_CustomFields_API.Controllers
{
    /// <summary>
    /// Controller for retrieving QuickBooks Items (products and services).
    /// Items are used as line items when creating invoices.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly ITokenManagerService _tokenManager;
        private readonly QuickBooksConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ItemController> _logger;

        public ItemController(
            ITokenManagerService tokenManager,
            IOptions<QuickBooksConfig> config,
            IHttpClientFactory httpClientFactory,
            ILogger<ItemController> logger)
        {
            _tokenManager = tokenManager;
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all active items (Services, Non-Inventory, and Inventory) from QuickBooks.
        /// These items can be used as line items when creating invoices.
        /// </summary>
        /// <returns>List of active items with their IDs, names, and unit prices</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetItems()
        {
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                if (token == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "No valid OAuth token found. Please authenticate first."
                    });
                }

                var client = _httpClientFactory.CreateClient();
                var baseUrl = _config.Environment == "production" ? _config.ProductionBaseUrl : _config.BaseUrl;
                
                // Query for active items that can be used on invoices (excludes categories, bundles, etc.)
                var query = "SELECT * FROM Item WHERE Active = true AND Type IN ('Service', 'NonInventory', 'Inventory') MAXRESULTS 1000";
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{baseUrl}/v3/company/{token.RealmId}/query?query={encodedQuery}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve items: {Content}", content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to retrieve items: {content}"
                    });
                }

                var itemResponse = JsonSerializer.Deserialize<QuickBooksItemQueryResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var items = itemResponse?.QueryResponse?.Item ?? new List<Item>();

                return Ok(new ApiResponse<List<Item>>
                {
                    Success = true,
                    Data = items,
                    Message = "Items retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to retrieve items: {ex.Message}"
                });
            }
        }
    }
}
