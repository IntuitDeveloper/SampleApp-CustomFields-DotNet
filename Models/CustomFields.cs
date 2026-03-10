namespace QuickBooks_CustomFields_API.Models
{
    // GraphQL Query Models
    public class GraphQLRequest
    {
        public string Query { get; set; } = string.Empty;
        public object? Variables { get; set; }
    }

    public class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public GraphQLError[]? Errors { get; set; }
    }

    public class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
        public GraphQLLocation[]? Locations { get; set; }
        public string[]? Path { get; set; }
    }

    public class GraphQLLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // CustomFields GraphQL Response Models (App Foundations API)
    public class AppFoundationsCustomFieldResponse
    {
        public AppFoundationsCustomFieldDefinitions? AppFoundationsCustomFieldDefinitions { get; set; }
    }

    // Create Custom Field Response Model
    public class AppFoundationsCreateCustomFieldResponse
    {
        public CustomFieldDefinitionNode? AppFoundationsCreateCustomFieldDefinition { get; set; }
    }

    // Update Custom Field Response Model
    public class AppFoundationsUpdateCustomFieldResponse
    {
        public CustomFieldDefinitionNode? AppFoundationsUpdateCustomFieldDefinition { get; set; }
    }

    public class AppFoundationsCustomFieldDefinitions
    {
        public CustomFieldEdge[]? Edges { get; set; }
        public PageInfo? PageInfo { get; set; }
    }

    public class CustomFieldEdge
    {
        public CustomFieldDefinitionNode? Node { get; set; }
    }

    public class CustomFieldDefinitionNode
    {
        public string Id { get; set; } = string.Empty;
        public string? LegacyID { get; set; } // Encoded format (Query 2)
        public string? LegacyIDV2 { get; set; } // Simple numeric format (Queries 3-6)
        public string Label { get; set; } = string.Empty;
        public CustomFieldAssociation[]? Associations { get; set; }
        public string DataType { get; set; } = string.Empty;
        public DropDownOption[]? DropDownOptions { get; set; }
        public bool Active { get; set; } = true;
        public CustomFieldDefinitionMetaModel? CustomFieldDefinitionMetaModel { get; set; }
    }

    public class DropDownOption
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public int Order { get; set; }
    }

    public class CustomFieldAssociation
    {
        public string AssociatedEntity { get; set; } = string.Empty;
        public bool Active { get; set; }
        public ValidationOptions? ValidationOptions { get; set; }
        public string[]? AllowedOperations { get; set; }
        public string AssociationCondition { get; set; } = string.Empty;
        public SubAssociation[]? SubAssociations { get; set; }
    }

    public class SubAssociation
    {
        public string AssociatedEntity { get; set; } = string.Empty;
        public bool Active { get; set; }
        public string[]? AllowedOperations { get; set; }
    }

    public class ValidationOptions
    {
        public bool Required { get; set; }
    }

    public class CustomFieldDefinitionMetaModel
    {
        public object? Suggested { get; set; }
    }



    public class MetaData
    {
        public DateTime? CreateTime { get; set; }
        public DateTime? LastUpdatedTime { get; set; }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string? StartCursor { get; set; }
        public string? EndCursor { get; set; }
    }

    // Mutation Models
    public class CustomFieldMutationResponse
    {
        public CustomFieldMutation? CustomFieldCreate { get; set; }
        public CustomFieldMutation? CustomFieldUpdate { get; set; }
        public CustomFieldMutation? CustomFieldDelete { get; set; }
    }

    public class CustomFieldMutation
    {
        public CustomFieldDefinitionNode? CustomField { get; set; }
        public MutationError[]? Errors { get; set; }
    }

    public class MutationError
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Field { get; set; }
    }

    // Input Models for Mutations
    public class CustomFieldInput
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
    }

    public class CustomFieldUpdateInput
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? StringValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
        public bool? Active { get; set; }
    }

    // Extension methods for converting between App Foundations and Legacy models
    public static class CustomFieldModelExtensions
    {
        /// <summary>
        /// Converts App Foundations CustomFieldDefinitionNode to Legacy CustomField model
        /// </summary>
        public static CustomField ToCustomField(this CustomFieldDefinitionNode node)
        {
            return new CustomField
            {
                Id = node.Id,
                Name = node.Label,
                Type = node.DataType,
                Active = node.Active,
                // Use LegacyIDV2 if available, otherwise try to parse LegacyID
                // MetaData_CreateTime and MetaData_LastUpdatedTime would need to come from elsewhere
            };
        }

        /// <summary>
        /// Gets the effective legacy ID from either LegacyID or LegacyIDV2
        /// </summary>
        public static string? GetEffectiveLegacyId(this CustomFieldDefinitionNode node)
        {
            // Prefer LegacyIDV2 (simple format) over LegacyID (encoded format)
            return node.LegacyIDV2 ?? ExtractLegacyIdFromEncoded(node.LegacyID);
        }

        /// <summary>
        /// Extracts the numeric ID from encoded LegacyID format
        /// </summary>
        private static string? ExtractLegacyIdFromEncoded(string? encodedLegacyId)
        {
            if (string.IsNullOrEmpty(encodedLegacyId))
                return null;

            // The encoded format appears to be base64 with numeric ID at the end
            // Example: "djQ6OTM0MTQ1MjU5OTA4NTI5MDovY29tbW9uL0N1c3RvbUZpZWxkRGVmaW5pdGlvbjo:1149549"
            var parts = encodedLegacyId.Split(':');
            return parts.Length > 0 ? parts[^1] : null; // Return last part
        }

        /// <summary>
        /// Gets all associated entity types for a custom field definition
        /// </summary>
        public static List<string> GetAssociatedEntityTypes(this CustomFieldDefinitionNode node)
        {
            var entityTypes = new List<string>();
            
            if (node.Associations != null)
            {
                foreach (var association in node.Associations)
                {
                    entityTypes.Add(association.AssociatedEntity);
                    
                    if (association.SubAssociations != null)
                    {
                        entityTypes.AddRange(association.SubAssociations.Select(sub => sub.AssociatedEntity));
                    }
                }
            }
            
            return entityTypes.Distinct().ToList();
        }

        /// <summary>
        /// Checks if the custom field is associated with a specific entity type
        /// </summary>
        public static bool IsAssociatedWith(this CustomFieldDefinitionNode node, string entityType)
        {
            return node.GetAssociatedEntityTypes().Contains(entityType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the validation requirements for a specific association
        /// </summary>
        public static bool IsRequiredFor(this CustomFieldDefinitionNode node, string associatedEntity)
        {
            var association = node.Associations?.FirstOrDefault(a => 
                string.Equals(a.AssociatedEntity, associatedEntity, StringComparison.OrdinalIgnoreCase));
            
            return association?.ValidationOptions?.Required ?? false;
        }
    }

    // Summary model for API responses that combines data from different query variations
    public class CustomFieldSummary
    {
        public string Id { get; set; } = string.Empty;
        public string? LegacyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool Active { get; set; }
        public List<string> AssociatedEntities { get; set; } = new();
        public List<AssociationSummary> Associations { get; set; } = new();
        public bool HasDropDownOptions { get; set; }
        public int DropDownOptionsCount { get; set; }
    }

    public class AssociationSummary
    {
        public string Entity { get; set; } = string.Empty;
        public bool Active { get; set; }
        public bool Required { get; set; }
        public List<string> SubEntities { get; set; } = new();
        public string Condition { get; set; } = string.Empty;
    }
}
