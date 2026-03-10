using Microsoft.AspNetCore.Mvc;
using QuickBooks_CustomFields_API.Models;
using QuickBooks_CustomFields_API.Services;

namespace QuickBooks_CustomFields_API.Controllers
{
    /// <summary>
    /// Controller for managing QuickBooks Custom Field Definitions using the App Foundations GraphQL API.
    /// Provides CRUD operations for custom fields that can be associated with transactions and contacts.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomFieldsController : ControllerBase
    {
        private readonly ICustomFieldService _customFieldService;
        private readonly ITokenManagerService _tokenManager;
        private readonly ILogger<CustomFieldsController> _logger;

        public CustomFieldsController(ICustomFieldService customFieldService, ITokenManagerService tokenManager, ILogger<CustomFieldsController> logger)
        {
            _customFieldService = customFieldService;
            _tokenManager = tokenManager;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all custom field definitions from QuickBooks using the App Foundations GraphQL API.
        /// Returns the full definition including associations, sub-associations, and dropdown options.
        /// </summary>
        /// <returns>List of CustomFieldDefinitionNode objects with complete field metadata</returns>
        [HttpGet("definitions")]
        public async Task<IActionResult> GetCustomFieldDefinitions()
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

                var definitions = await _customFieldService.GetCustomFieldDefinitionsAsync(token);
                return Ok(new ApiResponse<List<CustomFieldDefinitionNode>>
                {
                    Success = true,
                    Data = definitions,
                    Message = "Custom field definitions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to retrieve custom field definitions: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Retrieves a simplified summary view of all custom field definitions.
        /// Useful for displaying fields in a list/table format with key information only.
        /// </summary>
        /// <returns>List of CustomFieldSummary objects with essential field properties</returns>
        [HttpGet("summaries")]
        public async Task<IActionResult> GetCustomFieldSummaries()
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

                var summaries = await _customFieldService.GetCustomFieldSummariesAsync(token);
                return Ok(new ApiResponse<List<CustomFieldSummary>>
                {
                    Success = true,
                    Data = summaries,
                    Message = "Custom field summaries retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to retrieve custom field summaries: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Retrieves a single custom field definition by its unique ID.
        /// Used when editing a field to populate the form with current values.
        /// </summary>
        /// <param name="id">The unique identifier of the custom field definition</param>
        /// <returns>The complete CustomFieldDefinitionNode for the specified field</returns>
        [HttpGet("definitions/{id}")]
        public async Task<IActionResult> GetCustomFieldDefinitionById(string id)
        {
            _logger.LogInformation("========== EDIT BUTTON CLICKED ==========");
            _logger.LogInformation("Fetching custom field definition with ID: {FieldId}", id);
            
            try
            {
                var token = await _tokenManager.GetCurrentTokenAsync();
                if (token == null)
                {
                    _logger.LogWarning("Failed to retrieve custom field {FieldId} - No valid OAuth token found", id);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "No valid OAuth token found. Please authenticate first."
                    });
                }

                _logger.LogDebug("Calling custom field service to retrieve definition for ID: {FieldId}", id);
                var definition = await _customFieldService.GetCustomFieldDefinitionByIdAsync(token, id);
                
                if (definition == null)
                {
                    _logger.LogWarning("Custom field definition not found for ID: {FieldId}", id);
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Custom field definition with ID '{id}' not found"
                    });
                }

                _logger.LogInformation("✓ Successfully retrieved custom field definition");
                _logger.LogInformation("  ID: {FieldId}", definition.Id);
                _logger.LogInformation("  Label: {Label}", definition.Label);
                _logger.LogInformation("  DataType: {DataType}", definition.DataType);
                _logger.LogInformation("  Active: {Active}", definition.Active);
                
                if (definition.Associations != null && definition.Associations.Any())
                {
                    _logger.LogInformation("\n📋 Associations ({Count}):", definition.Associations.Length);
                    foreach (var association in definition.Associations)
                    {
                        _logger.LogInformation("  → Entity: {Entity}", association.AssociatedEntity);
                        _logger.LogInformation("    Active: {Active}", association.Active);
                        _logger.LogInformation("    Required: {Required}", association.ValidationOptions?.Required ?? false);
                        _logger.LogInformation("    Condition: {Condition}", association.AssociationCondition);
                        
                        if (association.SubAssociations != null && association.SubAssociations.Any())
                        {
                            _logger.LogInformation("    \n    🔗 SubAssociations ({Count}):", association.SubAssociations.Length);
                            foreach (var subAssoc in association.SubAssociations)
                            {
                                _logger.LogInformation("      • {SubEntity} (Active: {Active})", 
                                    subAssoc.AssociatedEntity, subAssoc.Active);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("    SubAssociations: None");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No associations found for this field");
                }
                
                _logger.LogInformation("=========================================\n");

                return Ok(new ApiResponse<CustomFieldDefinitionNode>
                {
                    Success = true,
                    Data = definition,
                    Message = "Custom field definition retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving custom field definition with ID: {FieldId}", id);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to retrieve custom field definition: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Creates a new custom field definition in QuickBooks.
        /// The field will be created with the specified name, data type, and entity associations.
        /// </summary>
        /// <param name="request">The custom field creation request containing name, type, and associations</param>
        /// <returns>The newly created custom field with its assigned ID</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCustomField([FromBody] CreateCustomFieldRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid request data",
                        Data = ModelState
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

                var customField = await _customFieldService.CreateCustomFieldDefinitionAsync(token, request);
                if (customField == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Failed to create custom field"
                    });
                }

                return CreatedAtAction(nameof(GetCustomFieldDefinitionById), new { id = customField.Id }, new ApiResponse<CustomField>
                {
                    Success = true,
                    Data = customField.ToCustomField(),
                    Message = "Custom field created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to create custom field: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Updates an existing custom field definition in QuickBooks.
        /// Can modify the field's name, active status, data type, and associations.
        /// Note: The legacyIDV2 is required for updates and is fetched automatically.
        /// </summary>
        /// <param name="id">The unique identifier of the custom field to update</param>
        /// <param name="request">The update request containing fields to modify</param>
        /// <returns>The updated custom field definition</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomField(string id, [FromBody] UpdateCustomFieldRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid request data",
                        Data = ModelState
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

                var customField = await _customFieldService.UpdateCustomFieldDefinitionAsync(token, id, request);
                if (customField == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Custom field with ID {id} not found or failed to update"
                    });
                }

                return Ok(new ApiResponse<CustomField>
                {
                    Success = true,
                    Data = customField.ToCustomField(),
                    Message = "Custom field updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to update custom field: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Deactivates a custom field definition in QuickBooks.
        /// Note: QuickBooks does not support permanent deletion of custom fields.
        /// This operation sets the field's active status to false (soft delete).
        /// </summary>
        /// <param name="id">The unique identifier of the custom field to deactivate</param>
        /// <returns>Success response if the field was deactivated</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomField(string id)
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

                var success = await _customFieldService.DeleteCustomFieldDefinitionAsync(token, id);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Custom field with ID {id} not found or failed to delete"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Custom field deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"Failed to delete custom field: {ex.Message}"
                });
            }
        }
    }
}
