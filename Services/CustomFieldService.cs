using QuickBooks_CustomFields_API.Models;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL;
using Microsoft.Extensions.Options;

namespace QuickBooks_CustomFields_API.Services
{
    public class CustomFieldService : ICustomFieldService
    {
        private readonly QuickBooksConfig _config;
        private readonly HttpClient _httpClient;

        public CustomFieldService(IOptions<QuickBooksConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        // App Foundations Custom Field Definitions API (New methods)
        public async Task<List<CustomFieldDefinitionNode>> GetCustomFieldDefinitionsAsync(OAuthToken token)
        {
            var query = @"
                query GetCustomFieldDefinitions {
                    appFoundationsCustomFieldDefinitions {
                        edges {
                            node {
                                id
                                legacyIDV2
                                label
                                associations {
                                    associatedEntity
                                    active
                                    validationOptions {
                                        required
                                    }
                                    allowedOperations
                                    associationCondition
                                    subAssociations {
                                        associatedEntity
                                        active
                                        allowedOperations
                                    }
                                }
                                dataType
                                dropDownOptions {
                                    id
                                    value
                                    active
                                    order
                                }
                                active
                                customFieldDefinitionMetaModel {
                                    suggested
                                }
                            }
                        }
                    }
                }";

            return await ExecuteAppFoundationsQueryAsync(token, query);
        }

        public async Task<List<CustomField>> GetCustomFieldDefinitionsAsLegacyAsync(OAuthToken token)
        {
            var definitions = await GetCustomFieldDefinitionsAsync(token);
            return definitions.Select(def => def.ToCustomField()).ToList();
        }

        public async Task<List<CustomFieldSummary>> GetCustomFieldSummariesAsync(OAuthToken token)
        {
            var definitions = await GetCustomFieldDefinitionsAsync(token);
            return definitions.Select(def => new CustomFieldSummary
            {
                Id = def.Id,
                LegacyId = def.GetEffectiveLegacyId(),
                Name = def.Label,
                DataType = def.DataType,
                Active = def.Active,
                AssociatedEntities = def.GetAssociatedEntityTypes(),
                Associations = def.Associations?.Select(assoc => new AssociationSummary
                {
                    Entity = assoc.AssociatedEntity,
                    Active = assoc.Active,
                    Required = assoc.ValidationOptions?.Required ?? false,
                    SubEntities = assoc.SubAssociations?.Select(sub => sub.AssociatedEntity).ToList() ?? new List<string>(),
                    Condition = assoc.AssociationCondition
                }).ToList() ?? new List<AssociationSummary>(),
                HasDropDownOptions = def.DropDownOptions?.Any() == true,
                DropDownOptionsCount = def.DropDownOptions?.Length ?? 0
            }).ToList();
        }

        public async Task<CustomFieldDefinitionNode?> GetCustomFieldDefinitionByIdAsync(OAuthToken token, string id)
        {
            var allDefinitions = await GetCustomFieldDefinitionsAsync(token);
            return allDefinitions.FirstOrDefault(def => def.Id == id);
        }

        // Redirect legacy method to new App Foundations API
        public async Task<List<CustomField>> GetCustomFieldsAsync(OAuthToken token)
        {
            return await GetCustomFieldDefinitionsAsLegacyAsync(token);
        }

        public async Task<List<CustomField>> GetCustomFieldsWithFilterAsync(OAuthToken token, string? name = null, string? type = null, bool? active = null, int? first = null, string? after = null)
        {
            // Get all definitions from App Foundations API and filter in memory
            // Note: App Foundations API doesn't support filtering in the query yet
            var allDefinitions = await GetCustomFieldDefinitionsAsLegacyAsync(token);
            
            var filtered = allDefinitions.AsQueryable();
            
            if (!string.IsNullOrEmpty(name))
                filtered = filtered.Where(cf => cf.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(type))
                filtered = filtered.Where(cf => string.Equals(cf.Type, type, StringComparison.OrdinalIgnoreCase));
            
            if (active.HasValue)
                filtered = filtered.Where(cf => cf.Active == active.Value);

            var result = filtered.ToList();
            
            // Simple pagination (not cursor-based like GraphQL)
            if (first.HasValue)
                result = result.Take(first.Value).ToList();
                
            return result;
        }

        public async Task<CustomFieldDefinitionNode?> CreateCustomFieldDefinitionAsync(OAuthToken token, CreateCustomFieldRequest request)
        {
            // Validate and sanitize dataType to prevent injection (only allow known enum values)
            // Note: BOOLEAN is not supported in AppFoundations_CustomExtensionDataType enum
            var validDataTypes = new[] { "STRING", "NUMBER", "DATE" };
            var dataType = request.Type?.ToUpperInvariant() ?? "STRING";
            if (!validDataTypes.Contains(dataType))
            {
                throw new ArgumentException($"Invalid data type: {request.Type}. Must be one of: {string.Join(", ", validDataTypes)}");
            }

            // Build allowedOperations based on PrintOnPage setting
            var allowedOpsString = request.PrintOnPage == true ? "[PRINT]" : "[]";
            
            // Build associations GraphQL string
            string associationsString;
            if (request.Associations?.Any() == true)
            {
                var assocStrings = request.Associations.Select(assoc => 
                {
                    var subAssocStrings = assoc.SubAssociations?.Select(sub => 
                        $@"{{ associatedEntity: ""{EscapeGraphQLString(sub)}"", active: true, allowedOperations: {allowedOpsString} }}"
                    ) ?? Enumerable.Empty<string>();
                    
                    var subAssocArray = subAssocStrings.Any() ? $"[{string.Join(", ", subAssocStrings)}]" : "[]";
                    
                    return $@"{{
                        associatedEntity: ""{EscapeGraphQLString(assoc.AssociatedEntity)}"",
                        active: {assoc.Active.ToString().ToLower()},
                        validationOptions: {{ required: {assoc.Required.ToString().ToLower()} }},
                        allowedOperations: {allowedOpsString},
                        associationCondition: INCLUDED,
                        subAssociations: {subAssocArray}
                    }}";
                });
                associationsString = $"[{string.Join(", ", assocStrings)}]";
            }
            else
            {
                var required = (request.Required ?? false).ToString().ToLower();
                associationsString = $@"[{{
                    associatedEntity: ""/transactions/Transaction"",
                    active: true,
                    validationOptions: {{ required: {required} }},
                    allowedOperations: {allowedOpsString},
                    associationCondition: INCLUDED,
                    subAssociations: [{{
                        associatedEntity: ""SALE_INVOICE"",
                        active: true,
                        allowedOperations: {allowedOpsString}
                    }}]
                }}]";
            }

            // Build the complete mutation with all values inline (to avoid JSON serialization of enums)
            var mutation = $@"
                mutation CreateCustomFieldDefinition {{
                    appFoundationsCreateCustomFieldDefinition(input: {{
                        label: ""{EscapeGraphQLString(request.Name)}"",
                        dataType: {dataType},
                        active: true,
                        associations: {associationsString},
                        dropDownOptions: []
                    }}) {{
                        id
                        label
                        dataType
                        active
                        associations {{
                            associatedEntity
                            active
                            validationOptions {{
                                required
                            }}
                            allowedOperations
                            associationCondition
                            subAssociations {{
                                associatedEntity
                                active
                                allowedOperations
                            }}
                        }}
                        dropDownOptions {{
                            id
                            value
                            active
                            order
                        }}
                    }}
                }}";

            // No variables needed - all values are inline in the mutation
            return await ExecuteAppFoundationsCreateMutationAsync(token, mutation, new { });
        }

        public async Task<CustomFieldDefinitionNode?> UpdateCustomFieldDefinitionAsync(OAuthToken token, string id, UpdateCustomFieldRequest request)
        {
            var mutation = @"
                mutation UpdateCustomFieldDefinition($input: AppFoundations_CustomFieldDefinitionUpdateInput!) {
                    appFoundationsUpdateCustomFieldDefinition(input: $input) {
                        id
                        legacyIDV2
                        label
                        dataType
                        active
                        associations {
                            associatedEntity
                            active
                            validationOptions {
                                required
                            }
                            allowedOperations
                            associationCondition
                            subAssociations {
                                associatedEntity
                                active
                                allowedOperations
                            }
                        }
                        dropDownOptions {
                            id
                            value
                            active
                            order
                        }
                    }
                }";

            // Build associations if provided
            object? associations = null;
            if (request.Associations?.Any() == true)
            {
                associations = request.Associations.Select(assoc => new 
                {
                    associatedEntity = assoc.AssociatedEntity,
                    active = assoc.Active,
                    validationOptions = new { required = assoc.Required },
                    allowedOperations = new string[0],
                    associationCondition = assoc.AssociationCondition,
                    subAssociations = assoc.SubAssociations?.Select(sub => new 
                    {
                        associatedEntity = sub,
                        active = true,
                        allowedOperations = new string[0]
                    }).ToArray() ?? new object[0]
                }).ToArray();
            }

            // Get current field to obtain legacyIDV2 (required for updates)
            var currentField = await GetCustomFieldDefinitionByIdAsync(token, id);
            if (currentField == null)
                throw new InvalidOperationException($"Custom field with ID {id} not found");

            // Build input object with only non-null values
            var input = new Dictionary<string, object?>();
            input["id"] = id;
            input["legacyIDV2"] = currentField.LegacyIDV2; // Required for updates
            
            // Label is required - use provided value or keep current
            input["label"] = !string.IsNullOrEmpty(request.Name) ? request.Name : currentField.Label;
            
            // Active status - use provided value or keep current
            input["active"] = request.Active ?? currentField.Active;
                
            // DataType - use provided value or keep current
            input["dataType"] = !string.IsNullOrEmpty(request.Type) ? request.Type : currentField.DataType;
                
            if (associations != null)
                input["associations"] = associations;

            var variables = new { input = input };
            
            return await ExecuteAppFoundationsUpdateMutationAsync(token, mutation, variables);
        }

        public async Task<bool> DeleteCustomFieldDefinitionAsync(OAuthToken token, string id)
        {
            // Since delete uses the same schema as update, we'll "delete" by setting active to false
            // This is a soft delete approach common in many APIs
            var deleteRequest = new UpdateCustomFieldRequest
            {
                Active = false
            };
            
            try 
            {
                var result = await UpdateCustomFieldDefinitionAsync(token, id, deleteRequest);
                return result != null && result.Active == false;
            }
            catch (Exception ex)
            {
                // Log detailed error information
                Console.WriteLine($"Delete (soft delete via update) failed for ID {id}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                
                return false; // Don't re-throw, just return false for delete operations
            }
        }

        // Redirect to new App Foundations API method  
        public async Task<CustomField?> GetCustomFieldByIdAsync(OAuthToken token, string id)
        {
            var definition = await GetCustomFieldDefinitionByIdAsync(token, id);
            return definition?.ToCustomField();
        }



        private async Task<CustomFieldDefinitionNode?> ExecuteAppFoundationsCreateMutationAsync(OAuthToken token, string mutation, object variables)
        {
            // App Foundations Custom Fields API uses production endpoints by default
            var graphQLEndpoint = _config.Environment.ToLower() == "production" 
                ? _config.ProductionGraphQLEndpoint 
                : _config.GraphQLEndpoint;
            
            using var graphQLClient = new GraphQLHttpClient(graphQLEndpoint, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var request = new GraphQL.GraphQLRequest { Query = mutation, Variables = variables };
            
            // Use specific response type for create mutations
            var response = await graphQLClient.SendQueryAsync<AppFoundationsCreateCustomFieldResponse>(request);

            if (response.Errors?.Any() == true)
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response.Data?.AppFoundationsCreateCustomFieldDefinition;
        }

        private async Task<CustomFieldDefinitionNode?> ExecuteAppFoundationsUpdateMutationAsync(OAuthToken token, string mutation, object variables)
        {
            // App Foundations Custom Fields API uses production endpoints by default
            var graphQLEndpoint = _config.Environment.ToLower() == "production" 
                ? _config.ProductionGraphQLEndpoint 
                : _config.GraphQLEndpoint;
            
            using var graphQLClient = new GraphQLHttpClient(graphQLEndpoint, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var request = new GraphQL.GraphQLRequest { Query = mutation, Variables = variables };
            
            // Use specific response type for update mutations
            var response = await graphQLClient.SendQueryAsync<AppFoundationsUpdateCustomFieldResponse>(request);

            if (response.Errors?.Any() == true)
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response.Data?.AppFoundationsUpdateCustomFieldDefinition;
        }

        private async Task<CustomFieldDefinitionNode?> ExecuteAppFoundationsMutationAsync(OAuthToken token, string mutation, object variables, string mutationName)
        {
            // App Foundations Custom Fields API uses production endpoints by default
            var graphQLEndpoint = _config.Environment.ToLower() == "production" 
                ? _config.ProductionGraphQLEndpoint 
                : _config.GraphQLEndpoint;
            
            using var graphQLClient = new GraphQLHttpClient(graphQLEndpoint, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var request = new GraphQL.GraphQLRequest { Query = mutation, Variables = variables };
            var response = await graphQLClient.SendQueryAsync<dynamic>(request);

            if (response.Errors?.Any() == true)
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            var mutationResult = response.Data?[mutationName];
            if (mutationResult?.errors != null)
            {
                var errorMessages = new List<string>();
                if (mutationResult.errors is System.Collections.IEnumerable errors)
                {
                    foreach (dynamic error in errors)
                    {
                        errorMessages.Add(error?.message?.ToString() ?? "Unknown error");
                    }
                }
                if (errorMessages.Count > 0)
                {
                    throw new Exception($"Mutation errors: {string.Join(", ", errorMessages)}");
                }
            }

            var customFieldDefinitionData = mutationResult?.customFieldDefinition;
            if (customFieldDefinitionData == null) return null;

            // TODO: Parse the dynamic response into CustomFieldDefinitionNode
            // This is a simplified version - you'll need to properly deserialize the response
            return new CustomFieldDefinitionNode
            {
                Id = customFieldDefinitionData.id?.ToString() ?? "",
                Label = customFieldDefinitionData.label?.ToString() ?? "",
                DataType = customFieldDefinitionData.dataType?.ToString() ?? "",
                Active = customFieldDefinitionData.active != null ? Convert.ToBoolean(customFieldDefinitionData.active) : true
                // TODO: Add associations and other properties from the response
            };
        }

        private async Task<List<CustomFieldDefinitionNode>> ExecuteAppFoundationsQueryAsync(OAuthToken token, string query)
        {
            // App Foundations Custom Fields API uses production endpoints by default
            var graphQLEndpoint = _config.Environment.ToLower() == "production" 
                ? _config.ProductionGraphQLEndpoint 
                : _config.GraphQLEndpoint;
            
            using var graphQLClient = new GraphQLHttpClient(graphQLEndpoint, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var request = new GraphQL.GraphQLRequest { Query = query };
            var response = await graphQLClient.SendQueryAsync<AppFoundationsCustomFieldResponse>(request);

            if (response.Errors?.Any() == true)
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            var edges = response.Data?.AppFoundationsCustomFieldDefinitions?.Edges ?? Array.Empty<CustomFieldEdge>();
            
            return edges.Where(edge => edge.Node != null)
                       .Select(edge => edge.Node!)
                       .ToList();
        }

        private async Task<CustomFieldDefinitionNode?> ExecuteAppFoundationsDeleteMutationAsync(OAuthToken token, string mutation, object variables)
        {
            // App Foundations Custom Fields API uses production endpoints by default
            var graphQLEndpoint = _config.Environment.ToLower() == "production" 
                ? _config.ProductionGraphQLEndpoint 
                : _config.GraphQLEndpoint;
            
            using var graphQLClient = new GraphQLHttpClient(graphQLEndpoint, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var request = new GraphQL.GraphQLRequest { Query = mutation, Variables = variables };
            
            // Use generic response type since delete might have different response structure
            var response = await graphQLClient.SendMutationAsync<dynamic>(request);

            if (response.Errors?.Any() == true)
            {
                throw new Exception($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            // Try to extract the deleted field information if available
            try 
            {
                var deleteData = response.Data?.appFoundationsDeleteCustomFieldDefinition;
                if (deleteData != null)
                {
                    return new CustomFieldDefinitionNode
                    {
                        Id = deleteData.id?.ToString() ?? string.Empty,
                        Active = deleteData.active ?? false
                    };
                }
            }
            catch 
            {
                // If we can't parse the response, but there were no errors, consider it successful
            }

            return new CustomFieldDefinitionNode { Id = "deleted" }; // Placeholder to indicate success
        }

        /// <summary>
        /// Escapes a string for use in a GraphQL query (handles quotes and special characters)
        /// </summary>
        private static string EscapeGraphQLString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

