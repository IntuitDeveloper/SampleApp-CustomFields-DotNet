using System.ComponentModel.DataAnnotations;

namespace QuickBooks_CustomFields_API.Models
{
    public class OAuthToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5); // 5-minute buffer
    }

    public class QuickBooksConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string DiscoveryDocument { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string GraphQLEndpoint { get; set; } = string.Empty;
        public string ProductionBaseUrl { get; set; } = string.Empty;
        public string ProductionGraphQLEndpoint { get; set; } = string.Empty;
        public string Environment { get; set; } = "sandbox";
        public List<string> CustomFieldScopes { get; set; } = new List<string>();
        public int MinorVersion { get; set; } = 75;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public class CustomField
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
        public DateTime? MetaData_CreateTime { get; set; }
        public DateTime? MetaData_LastUpdatedTime { get; set; }
        public bool Active { get; set; } = true;
    }

    public class CreateCustomFieldRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty; // STRING, NUMBER, DATE, BOOLEAN
        
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
        
        // Association configuration
        public List<CreateAssociationRequest>? Associations { get; set; }
        public bool? Required { get; set; } = false;
        public bool? PrintOnPage { get; set; } = false;
    }

    public class CreateAssociationRequest
    {
        [Required]
        public string AssociatedEntity { get; set; } = string.Empty; // e.g., "/transactions/Transaction", "/network/Contact"
        
        public bool Active { get; set; } = true;
        public bool Required { get; set; } = false;
        public string AssociationCondition { get; set; } = "INCLUDED"; // INCLUDED, EXCLUDED
        public List<string>? SubAssociations { get; set; } // e.g., ["SALE_INVOICE", "SALE_ESTIMATE"]
    }

    public class UpdateCustomFieldRequest
    {
        public string? Name { get; set; }
        public string? Type { get; set; } // STRING, NUMBER, DATE, BOOLEAN
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
        public bool? Active { get; set; }
        
        // Association updates
        public List<CreateAssociationRequest>? Associations { get; set; }
    }
}
