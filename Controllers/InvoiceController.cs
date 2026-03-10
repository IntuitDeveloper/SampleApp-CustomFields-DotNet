using Microsoft.AspNetCore.Mvc;
using QuickBooks_CustomFields_API.Models;
using QuickBooks_CustomFields_API.Services;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace QuickBooks_CustomFields_API.Controllers
{
    /// <summary>
    /// Controller for managing QuickBooks Invoices using the REST API v3.
    /// Provides operations to list, create, and update invoices with custom field support.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly ITokenManagerService _tokenManager;
        private readonly QuickBooksConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(
            ITokenManagerService tokenManager,
            IOptions<QuickBooksConfig> config,
            IHttpClientFactory httpClientFactory,
            ILogger<InvoiceController> logger)
        {
            _tokenManager = tokenManager;
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of invoices from QuickBooks, ordered by creation date descending.
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of invoices per page (default: 10)</param>
        /// <returns>Paginated list of invoices with metadata</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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

                // Calculate start position for pagination
                var startPosition = (page - 1) * pageSize + 1;
                
                var client = _httpClientFactory.CreateClient();
                var baseUrl = _config.Environment == "production" ? _config.ProductionBaseUrl : _config.BaseUrl;
                var query = $"SELECT * FROM Invoice ORDERBY MetaData.CreateTime DESC STARTPOSITION {startPosition} MAXRESULTS {pageSize}";
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{baseUrl}/v3/company/{token.RealmId}/query?query={encodedQuery}&minorversion={_config.MinorVersion}&include=enhancedAllCustomFields";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve invoices: {Content}", content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to retrieve invoices: {content}"
                    });
                }

                var invoiceResponse = JsonSerializer.Deserialize<QuickBooksInvoiceQueryResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var invoices = invoiceResponse?.QueryResponse?.Invoice ?? new List<Invoice>();
                var totalCount = invoiceResponse?.QueryResponse?.MaxResults ?? 0;
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new 
                    {
                        Invoices = invoices,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        HasMore = invoices.Count == pageSize
                    },
                    Message = "Invoices retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Retrieves all customers from QuickBooks for use in invoice creation.
        /// Limited to 100 customers for performance.
        /// </summary>
        /// <returns>List of customers with their IDs and display names</returns>
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
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
                var query = "SELECT * FROM Customer MAXRESULTS 100";
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{baseUrl}/v3/company/{token.RealmId}/query?query={encodedQuery}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to retrieve customers: {Content}", content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to retrieve customers: {content}"
                    });
                }

                var customerResponse = JsonSerializer.Deserialize<QuickBooksCustomerQueryResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(new ApiResponse<List<Customer>>
                {
                    Success = true,
                    Data = customerResponse?.QueryResponse?.Customer ?? new List<Customer>(),
                    Message = "Customers retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to retrieve customers: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Creates a new invoice in QuickBooks with line items and optional custom field values.
        /// Uses the enhancedAllCustomFields parameter to return custom field data in response.
        /// </summary>
        /// <param name="request">Invoice creation request containing customer, line items, and optional custom field</param>
        /// <returns>The newly created invoice with its QuickBooks ID</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateRequest request)
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
                var url = $"{baseUrl}/v3/company/{token.RealmId}/invoice?minorversion={_config.MinorVersion}&include=enhancedAllCustomFields";

                // Build invoice payload with required and optional fields
                var invoicePayload = new
                {
                    CustomerRef = new { value = request.CustomerId },
                    TxnDate = request.InvoiceDate,
                    DueDate = request.DueDate,
                    // Transform line items to QuickBooks SalesItemLineDetail format
                    Line = request.LineItems.Select(item => new
                    {
                        Amount = item.Amount,
                        DetailType = "SalesItemLineDetail",
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = item.ItemId },
                            Qty = item.Quantity,
                            UnitPrice = item.UnitPrice
                        },
                        Description = item.Description
                    }).ToArray(),
                    // Build custom fields array from all provided custom fields
                    CustomField = request.CustomFields?.Any() == true
                        ? request.CustomFields.Select(cf => BuildCustomFieldObject(cf)).ToArray()
                        : null,
                    CustomerMemo = !string.IsNullOrEmpty(request.Notes)
                        ? new { value = request.Notes }
                        : null
                };

                var jsonPayload = JsonSerializer.Serialize(invoicePayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                _logger.LogInformation("Creating invoice with payload: {Payload}", jsonPayload);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                httpRequest.Headers.Add("Accept", "application/json");
                httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create invoice: {Content}", content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to create invoice: {content}"
                    });
                }

                var invoiceResponse = JsonSerializer.Deserialize<QuickBooksInvoiceResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(new ApiResponse<Invoice>
                {
                    Success = true,
                    Data = invoiceResponse?.Invoice,
                    Message = "Invoice created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Updates an existing invoice in QuickBooks.
        /// Requires InvoiceId and SyncToken for optimistic concurrency control.
        /// Uses sparse update to only modify specified fields.
        /// </summary>
        /// <param name="request">Invoice update request with InvoiceId, SyncToken, and fields to update</param>
        /// <returns>The updated invoice</returns>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceId) || string.IsNullOrEmpty(request.SyncToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invoice ID and SyncToken are required for updates"
                    });
                }

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
                var url = $"{baseUrl}/v3/company/{token.RealmId}/invoice?minorversion={_config.MinorVersion}&include=enhancedAllCustomFields";

                // Build sparse update payload - only specified fields will be updated
                // sparse=true tells QuickBooks to merge with existing data instead of replacing
                var invoicePayload = new
                {
                    sparse = true,
                    Id = request.InvoiceId,
                    SyncToken = request.SyncToken,  // Required for optimistic concurrency
                    CustomerRef = new { value = request.CustomerId },
                    TxnDate = request.InvoiceDate,
                    DueDate = request.DueDate,
                    // Line items require special handling for updates
                    Line = request.LineItems.Select(item =>
                    {
                        var line = new Dictionary<string, object>
                        {
                            { "Amount", item.Amount },
                            { "DetailType", "SalesItemLineDetail" },
                            { "SalesItemLineDetail", new
                                {
                                    ItemRef = new { value = item.ItemId },
                                    Qty = item.Quantity,
                                    UnitPrice = item.UnitPrice
                                }
                            },
                            { "Description", item.Description }
                        };
                        
                        // Include Line.Id for existing line items (required for updates)
                        // Without the Id, QuickBooks will create a new line instead of updating
                        if (!string.IsNullOrEmpty(item.LineId))
                        {
                            line["Id"] = item.LineId;
                        }
                        
                        return line;
                    }).ToArray(),
                    // Build custom fields array from all provided custom fields
                    CustomField = request.CustomFields?.Any() == true
                        ? request.CustomFields.Select(cf => BuildCustomFieldObject(cf)).ToArray()
                        : null,
                    CustomerMemo = !string.IsNullOrEmpty(request.Notes)
                        ? new { value = request.Notes }
                        : null
                };

                var jsonPayload = JsonSerializer.Serialize(invoicePayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                _logger.LogInformation("Updating invoice {InvoiceId} with payload: {Payload}", request.InvoiceId, jsonPayload);

                // QuickBooks uses POST for both create and update operations
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
                httpRequest.Headers.Add("Accept", "application/json");
                httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update invoice: {Content}", content);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to update invoice: {content}"
                    });
                }

                var invoiceResponse = JsonSerializer.Deserialize<QuickBooksInvoiceResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(new ApiResponse<Invoice>
                {
                    Success = true,
                    Data = invoiceResponse?.Invoice,
                    Message = "Invoice updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Builds a custom field object with the appropriate value property based on data type.
        /// Note: QuickBooks API accepts StringValue for all data types including NUMBER.
        /// Using StringValue ensures the value is properly stored and returned by the API.
        /// </summary>
        private Dictionary<string, object?> BuildCustomFieldObject(InvoiceCustomField cf)
        {
            var customField = new Dictionary<string, object?>
            {
                { "DefinitionId", cf.DefinitionId },
                { "Name", cf.Name }
            };

            // Use StringValue for all types - QuickBooks API handles the conversion
            // and reliably returns the value in GET responses (as NumberValue/DateValue)
            customField["StringValue"] = cf.Value;

            return customField;
        }
    }
}
