using QuickBooks_CustomFields_API.Models;

namespace QuickBooks_CustomFields_API.Services
{
    public interface ICustomFieldService
    {
        // App Foundations Custom Field Definitions API
        Task<List<CustomFieldDefinitionNode>> GetCustomFieldDefinitionsAsync(OAuthToken token);
        Task<List<CustomField>> GetCustomFieldDefinitionsAsLegacyAsync(OAuthToken token);
        Task<List<CustomFieldSummary>> GetCustomFieldSummariesAsync(OAuthToken token);
        Task<CustomFieldDefinitionNode?> GetCustomFieldDefinitionByIdAsync(OAuthToken token, string id);
        
        // Legacy compatibility methods (redirected to App Foundations API)
        Task<List<CustomField>> GetCustomFieldsAsync(OAuthToken token);
        Task<List<CustomField>> GetCustomFieldsWithFilterAsync(OAuthToken token, string? name = null, string? type = null, bool? active = null, int? first = null, string? after = null);
        Task<CustomField?> GetCustomFieldByIdAsync(OAuthToken token, string id);
        
        // Custom Field Mutations (App Foundations API)
        Task<CustomFieldDefinitionNode?> CreateCustomFieldDefinitionAsync(OAuthToken token, CreateCustomFieldRequest request);
        Task<CustomFieldDefinitionNode?> UpdateCustomFieldDefinitionAsync(OAuthToken token, string id, UpdateCustomFieldRequest request);
        Task<bool> DeleteCustomFieldDefinitionAsync(OAuthToken token, string id);
    }
}
