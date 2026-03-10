# QuickBooks CustomFields API

A .NET 9 Web API for managing QuickBooks Custom Field Definitions and Invoices using the App Foundations GraphQL API and QuickBooks REST API v3.

## Features

### Custom Field Management
- **Complete CRUD Operations**: Create, Read, Update, Delete (soft delete) for QuickBooks custom field definitions
- **App Foundations API Integration**: Full implementation using QuickBooks App Foundations GraphQL API
- **Advanced Association Management**: Complex entity relationships (Transactions, Contacts, Vendors, Customers)
- **Production-Ready Soft Delete**: Safe deactivation-based deletion preserving data integrity
- **Schema Compliance**: Complete `AppFoundations_CustomFieldDefinitionInput` schema support
- **Automatic Field Management**: Auto-retrieval of required fields (`legacyIDV2`, field preservation)
- **Business Rule Enforcement**: Association validation and mutual exclusivity checking

### Invoice Management
- **Invoice CRUD Operations**: Create, read, update invoices with QuickBooks REST API v3
- **Custom Fields Integration**: Attach custom field values to invoices with `DefinitionId` and `Name`
- **Multi-Line Item Support**: Full support for multiple line items with proper `Line.Id` handling
- **Items/Services Management**: Dynamic item/service selection from QuickBooks
- **Real-time Calculations**: Automatic line item and invoice total calculations
- **Interactive UI**: Modern web interface for invoice and custom field management

### Technical Features
- **Dual API Support**: App Foundations GraphQL + REST API v3
- **OAuth 2.0 with App Foundations Scopes**: Secure authentication with granular permissions
- **RESTful API**: Clean REST endpoints for seamless integration
- **Comprehensive Error Handling**: GraphQL error parsing and detailed validation messages
- **Interactive Documentation**: Swagger UI with complete CRUD testing examples
- **Modern Web UI**: Bootstrap 5 interface with real-time updates

## SKU and Data Type Support

Custom field capabilities vary by QuickBooks subscription tier:

| SKU | Transactional Custom Fields | Customer/Entity Custom Fields |
|-----|----------------------------|------------------------------|
| **Simple Start** | Text, Dropdown; limit 1 per transaction | Text, Number, Date, Dropdown; up to 30 customer fields (from July 1, 2025) |
| **Essentials** | Text, Dropdown; limit 4 per transaction (historically 3 total) | Same as Simple Start |
| **Plus** | Text, Dropdown; limit 4 per transaction (historically 3 total) | Same as Simple Start |
| **Advanced** | Text, Number, Date, Dropdown; up to 12 per transaction | Text, Number, Date, Dropdown; up to 12 per entity (customer/vendor/project) |
| **Intuit Enterprise Suite (IES)** | Text, Number, Date, Dropdown; up to 12 per transaction | Text, Number, Date, Dropdown; up to 12 per entity |

## Prerequisites

Before running this application, ensure you have the following installed:

- **.NET 9 SDK** or later - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **QuickBooks Developer Account** - [Sign up](https://developer.intuit.com/)
- **QuickBooks App** - Created in your developer account with:
  - OAuth 2.0 configured
  - App Foundations scopes enabled
  - Redirect URI configured
- **ngrok** - For production redirect URL


### QuickBooks App Setup Requirements

1. **Create a QuickBooks App**:
   - Go to [Intuit Developer Portal](https://developer.intuit.com/)
   - Create a new app or use an existing one
   - Note your `Client ID` and `Client Secret`

2. **Configure OAuth Settings**:
   - Set Redirect URI: `http://localhost:5039/api/oauth/callback`
   - Enable required scopes:
     - `app-foundations.custom-field-definitions.read`
     - `app-foundations.custom-field-definitions`
     - `com.intuit.quickbooks.accounting`

3. **Environment Note**:
   - App Foundations Custom Field API requires **Production** environment
   - Invoice operations work in both Sandbox and Production
   - Configure environment in `appsettings.json`

## Dependencies

- **GraphQL.Client** - GraphQL client for .NET
- **IppDotNetSdkForQuickBooksApiV3** - Official Intuit .NET SDK for OAuth
- **Newtonsoft.Json** - JSON serialization
- **Microsoft.AspNetCore** - Web API framework


## API Endpoints

### **Core Custom Field Operations (Essential)**
- `GET /api/customfields/definitions` - Get all custom field definitions with associations
- `GET /api/customfields/definitions/{id}` - Get specific custom field definition by ID
- `GET /api/customfields/summaries` - Get simplified summaries (optimized for UI)
- `POST /api/customfields` - Create a new custom field definition
- `PUT /api/customfields/{id}` - Update an existing custom field definition
- `DELETE /api/customfields/{id}` - Delete (deactivate) a custom field definition

### **Invoice Operations**
- `GET /api/invoice/list?page={page}&pageSize={pageSize}` - Get paginated invoice list
- `POST /api/invoice/create` - Create a new invoice with line items and custom fields
- `PUT /api/invoice/update` - Update an existing invoice
- `GET /api/customer/list` - Get all active customers
- `GET /api/item/list` - Get all active items/services

### **OAuth Authentication (Required)**
- `GET /api/oauth/authorize` - Initiate OAuth flow
- `GET /api/oauth/callback` - OAuth callback handler
- `GET /api/oauth/status` - Check authentication status
- `POST /api/oauth/refresh` - Refresh access token
- `POST /api/oauth/disconnect` - Disconnect from QuickBooks


## Getting Started

### 1. Clone the Repository

```bash
# Clone the repository
git clone <repository-url>

# Navigate to the project directory
cd SampleApp-CustomFields-DotNet
```

### 2. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

### 3. Configure the Application

Update `appsettings.json` with your QuickBooks app credentials:

**Important**: The App Foundations Custom Field Definitions API requires the **production environment** as it's not available in sandbox mode.

```json
{
  "QuickBooks": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5039/api/oauth/callback",
    "DiscoveryDocument": "https://appcenter.intuit.com/api/v1/connection/oauth2",
    "BaseUrl": "https://sandbox-quickbooks.api.intuit.com",
    "GraphQLEndpoint": "https://public.api.intuit.com/2020-04/graphql",
    "ProductionGraphQLEndpoint": "https://qb.api.intuit.com/graphql",
    "Environment": "production",
    "CustomFieldScopes": [
      "app-foundations.custom-field-definitions.read",
      "app-foundations.custom-field-definitions",
      "com.intuit.quickbooks.accounting"
    ]
  }
}
```

**Configuration Steps**:
1. Replace `YOUR_CLIENT_ID` with your QuickBooks app's Client ID
2. Replace `YOUR_CLIENT_SECRET` with your QuickBooks app's Client Secret
3. Verify the `RedirectUri` matches your QuickBooks app configuration
4. Set `Environment` to `"production"` for custom fields or `"sandbox"` for testing invoices only

### 4. Run the Application

```bash
# Run on specific port
dotnet run --urls "http://localhost:5039"

# Or use default settings
dotnet run
```

The application will start and listen on `http://localhost:5039`

### 5. Access the Application

**Web UI**:
- Open your browser to `http://localhost:5039`
- You'll see the interactive web interface

**Swagger Documentation**:
- Navigate to `http://localhost:5039/swagger`
- Interactive API documentation and testing

### 6. Authenticate with QuickBooks

1. Click "Connect to QuickBooks" in the web UI or navigate to `/api/oauth/authorize`
2. Sign in with your QuickBooks account
3. Authorize the application
4. You'll be redirected back to the application
5. Check authentication status at `/api/oauth/status`

### 7. Start Using the Application

**Via Web UI**:
- Manage custom fields in the Custom Fields tab
- Create and manage invoices in the Invoices tab

**Via API**:
- Use Swagger UI to test API endpoints
- Use curl commands from the Testing section below

## Custom Field Definition Features

The App Foundations API provides advanced custom field capabilities:

### Data Types
- **STRING**: Text values
- **NUMBER**: Numeric values (stored as string, displayed as number)
- **DATE**: Date values (API format: yyyy-mm-dd, UI display: mm/dd/yyyy)

### Entity Associations

Custom fields can be associated with different QuickBooks entities and their sub-types. The API supports flexible association configurations:

#### **Supported Entity Types**
- **Transactions**: `/transactions/Transaction`
  - `SALE_INVOICE` - Sales invoices
  - `SALE_ESTIMATE` - Sales estimates/quotes  
  - `PURCHASE_ORDER` - Purchase orders
  - And more transaction types...
- **Contacts**: `/network/Contact`
  - `CUSTOMER` - Customer contacts
  - `VENDOR` - Vendor contacts
  - `EMPLOYEE` - Employee contacts

#### **Association Configuration**
```json
{
  "name": "Custom Field Name",
  "type": "STRING",
  "associations": [
    {
      "associatedEntity": "/transactions/Transaction",
      "active": true,
      "required": false,
      "associationCondition": "INCLUDED",
      "subAssociations": ["SALE_INVOICE", "SALE_ESTIMATE"]
    }
  ]
}
```

#### **Association Features**
- **✅ Multiple Sub-Entities**: Associate with multiple sub-types within the same entity
- **✅ Required/Optional Fields**: Control field validation per association
- **✅ Active State Management**: Enable/disable associations dynamically
- **✅ Conditional Logic**: `INCLUDED` or `EXCLUDED` association conditions
- **❌ Mixed Entity Types**: Cannot combine Contact and Transaction associations in one field

#### **Business Rules**
- **Single Entity Type**: Each custom field can only associate with one main entity type
- **Multiple Sub-Associations**: Within an entity type, you can associate with multiple sub-types
- **Mutual Exclusivity**: Some sub-associations are mutually exclusive (API will return specific errors)

## App Foundations GraphQL Operations

The service uses the App Foundations API with complete CRUD support:

### **Queries**
- **`appFoundationsCustomFieldDefinitions`**: Primary query for retrieving all custom field definitions
- **Association Data**: Complex entity relationships with sub-associations
- **Legacy ID Support**: Handles both `legacyID` and `legacyIDV2` formats
- **Edge-based Results**: GraphQL edges/nodes pattern for pagination

### **Mutations**

#### **Create: `appFoundationsCreateCustomFieldDefinition`**
- **Input**: `AppFoundations_CustomFieldDefinitionCreateInput!`
- **Required**: `label`, `dataType`, `active`, `associations`
- **Returns**: Complete field definition with generated ID
- **Features**: Default associations, custom entity relationships

#### **Update: `appFoundationsUpdateCustomFieldDefinition`**
- **Input**: `AppFoundations_CustomFieldDefinitionUpdateInput!`
- **Required**: `id`, `legacyIDV2`, `label`, `active`, `dataType`
- **Returns**: Updated field definition
- **Features**: Partial updates, field preservation, association modification

#### **Delete: Soft Delete via Update**
- **Implementation**: Uses update mutation with `active: false`
- **Reason**: App Foundations API doesn't provide direct delete mutation
- **Benefits**: Data preservation, audit trails, reversibility
- **Schema**: Same as update but sets inactive status

### **Schema Compliance**
- **Production Environment**: App Foundations API requires production endpoints
- **GraphQL Endpoint**: `https://qb.api.intuit.com/graphql`
- **Required Scopes**: `app-foundations.custom-field-definitions.read`, `app-foundations.custom-field-definitions`
- **Authentication**: OAuth 2.0 Bearer tokens

### **Data Models**
- **CustomFieldDefinitionNode**: Primary App Foundations model
- **Legacy Compatibility**: Automatic conversion to legacy `CustomField` format
- **Summary Views**: Simplified `CustomFieldSummary` for UI consumption
- **Extension Methods**: Rich helper methods for association management and data conversion

### **Error Handling**
- **GraphQL Errors**: Detailed error messages for schema validation
- **Field Validation**: Required field checking and data type validation
- **Association Rules**: Business rule enforcement for entity relationships
- **Network Resilience**: Retry logic and connection management

## Authentication

The application uses OAuth 2.0 with App Foundations scopes:

### Required Scopes
- `app-foundations.custom-field-definitions.read` - Read custom field definitions
- `app-foundations.custom-field-definitions` - Full custom field definition management
- `com.intuit.quickbooks.accounting` - General QuickBooks access

### Security Features
- Automatic token refresh
- Session-based state management
- Secure token storage
- CSRF protection with state parameter
- Production/sandbox environment support

## CRUD Operations Guide

The QuickBooks CustomFields API provides complete Create, Read, Update, and Delete (CRUD) operations using the App Foundations GraphQL API.

### **CREATE - Custom Field Definitions**

Creates new custom fields with flexible association configurations using `AppFoundations_CustomFieldDefinitionCreateInput`.

#### **Required Fields**
- `label` (String): Field name/label
- `dataType` (String): `STRING`, `NUMBER`, `DATE`
- `active` (Boolean): Field activation status
- `associations` (Array): Entity associations (optional, defaults to Transaction/SALE_INVOICE)

#### **Schema: `appFoundationsCreateCustomFieldDefinition`**
```graphql
mutation CreateCustomFieldDefinition($input: AppFoundations_CustomFieldDefinitionCreateInput!) {
  appFoundationsCreateCustomFieldDefinition(input: $input) {
    id
    label
    dataType
    active
    associations { ... }
    dropDownOptions { ... }
  }
}
```

#### **Examples:**

**Default Association (Backward Compatibility)**
```bash
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Default Field",
    "type": "STRING"
  }'
```
*Creates: Transaction/SALE_INVOICE association (default behavior)*

**Contact/Vendor Association**
```bash
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Vendor Contact Field",
    "type": "STRING",
    "associations": [
      {
        "associatedEntity": "/network/Contact",
        "active": true,
        "required": false,
        "associationCondition": "INCLUDED",
        "subAssociations": ["VENDOR"]
      }
    ]
  }'
```

**Multiple Transaction Types**
```bash
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Multi Transaction Field",
    "type": "STRING",
    "associations": [
      {
        "associatedEntity": "/transactions/Transaction",
        "active": true,
        "required": false,
        "associationCondition": "INCLUDED",
        "subAssociations": ["SALE_INVOICE", "SALE_ESTIMATE"]
      }
    ]
  }'
```

#### **Business Rules**
- ✅ **Single Entity Type**: Each field can only associate with one main entity type
- ✅ **Multiple Sub-Associations**: Within an entity, multiple sub-types are supported
- ❌ **Mixed Entity Types**: Cannot combine Contact and Transaction associations in one field
- ⚠️ **Mutual Exclusivity**: Some sub-associations are mutually exclusive (API returns specific errors)

---

### **READ - Custom Field Definitions**

Retrieves custom field definitions with full association details.

#### **Endpoints:**
- `GET /api/customfields/definitions` - All definitions with associations
- `GET /api/customfields/summaries` - Simplified summary view
- `GET /api/customfields/definitions/{id}` - Specific field by ID
- `GET /api/customfields` - Legacy format (auto-converted)

#### **Schema: `appFoundationsCustomFieldDefinitions`**
```graphql
query GetCustomFieldDefinitions {
  appFoundationsCustomFieldDefinitions {
    edges {
      node {
        id
        legacyIDV2
        label
        dataType
        active
        associations { ... }
        dropDownOptions { ... }
      }
    }
  }
}
```

#### **Examples:**
```bash
# Get all definitions with associations
curl -X GET "http://localhost:5039/api/customfields/definitions"

# Get simplified summaries
curl -X GET "http://localhost:5039/api/customfields/summaries"

# Get specific field
curl -X GET "http://localhost:5039/api/customfields/definitions/{id}"

# Verify associations
curl -X GET "http://localhost:5039/api/customfields/summaries" | jq '.data[] | {name, associatedEntities, associations}'
```

---

### **UPDATE - Custom Field Definitions**

Updates existing custom fields using `AppFoundations_CustomFieldDefinitionUpdateInput`.

#### **Required Fields (Auto-Retrieved)**
- `id` (ID): Custom field identifier
- `legacyIDV2` (ID): Legacy identifier (automatically retrieved)
- `label` (String): Current or new label (preserved if not provided)
- `active` (Boolean): Current or new status (preserved if not provided)
- `dataType` (String): Current or new data type (preserved if not provided)

#### **Schema: `appFoundationsUpdateCustomFieldDefinition`**
```graphql
mutation UpdateCustomFieldDefinition($input: AppFoundations_CustomFieldDefinitionUpdateInput!) {
  appFoundationsUpdateCustomFieldDefinition(input: $input) {
    id
    legacyIDV2
    label
    dataType
    active
    associations { ... }
    dropDownOptions { ... }
  }
}
```

#### **Examples:**

**Simple Name Update**
```bash
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated Field Name"}'
```

**Deactivate Field**
```bash
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{"active": false}'
```

**Update Associations**
```bash
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Field",
    "associations": [
      {
        "associatedEntity": "/network/Contact",
        "active": true,
        "required": true,
        "associationCondition": "INCLUDED",
        "subAssociations": ["CUSTOMER"]
      }
    ]
  }'
```

**Multiple Field Update**
```bash
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Multi-Update Field",
    "active": true,
    "type": "STRING"
  }'
```

#### **Update Behavior**
- ✅ **Partial Updates**: Only specified fields are updated
- ✅ **Field Preservation**: Non-specified fields retain current values
- ✅ **Association Addition**: New associations appear to be additive (preserves existing)
- ✅ **Data Type Validation**: Invalid types are rejected with clear errors
- ⚠️ **Required Fields**: `legacyIDV2` and `label` are required (auto-handled by API)

---

### **DELETE - Custom Field Definitions**

Deletes (deactivates) custom fields using the update schema as a soft delete.

#### **Implementation: Soft Delete**
Since the App Foundations API doesn't provide a direct delete mutation, deletion is implemented as an update operation that sets `active: false`. This is a safe, production-ready approach that preserves data integrity.

#### **Schema: Uses Update Mutation**
```graphql
# Same as update, but sets active: false
mutation SoftDeleteCustomFieldDefinition($input: AppFoundations_CustomFieldDefinitionUpdateInput!) {
  appFoundationsUpdateCustomFieldDefinition(input: $input) {
    id
    active  # Will be false after soft delete
  }
}
```

#### **Examples:**

**Delete Custom Field**
```bash
curl -X DELETE "http://localhost:5039/api/customfields/{id}"
```

**Verify Deletion**
```bash
# Check field is now inactive
curl -X GET "http://localhost:5039/api/customfields/summaries" | jq '.data[] | select(.id == "{id}") | .active'
```

#### **Delete Behavior**
- ✅ **Soft Delete**: Sets `active: false` instead of permanent removal
- ✅ **Data Preservation**: All field data, associations, and metadata preserved
- ✅ **Audit Trail**: Maintains history of deleted fields
- ✅ **Reactivation**: Fields can be restored by updating `active: true`
- ✅ **Error Handling**: Non-existent fields return proper 404 errors
- ✅ **Association Preservation**: Complex associations remain intact

#### **Benefits of Soft Delete**
- **Data Safety**: No permanent data loss
- **Compliance**: Maintains audit trails for regulatory requirements
- **Reversibility**: Accidental deletions can be undone
- **Performance**: Faster than hard deletes in production systems
- **Integration**: Works seamlessly with existing update infrastructure

---

## Invoice Management with Custom Fields

The application provides complete invoice management with integrated custom field support, combining QuickBooks REST API v3 with App Foundations Custom Fields.

### **Invoice CRUD Operations**

#### **CREATE - Invoice with Custom Fields**

Creates new invoices with line items and custom field values.

**Endpoint:** `POST /api/invoice/create`

**Required Fields:**
- `customerId` (string): QuickBooks customer ID
- `invoiceDate` (string): Invoice date (YYYY-MM-DD)
- `lineItems` (array): At least one line item required
  - `itemId` (string): QuickBooks item/service ID
  - `description` (string): Line item description
  - `quantity` (number): Item quantity
  - `unitPrice` (number): Price per unit
  - `amount` (number): Total amount (quantity × unitPrice)

**Optional Fields:**
- `dueDate` (string): Payment due date
- `customFields` (array): Array of custom field values
  - `definitionId` (string): Custom field DefinitionId (from `legacyIDV2`)
  - `name` (string): Custom field name/label
  - `value` (string): Custom field value
  - `dataType` (string): Data type (STRING, NUMBER, DATE)
- `notes` (string): Invoice notes/memo

**Example:**
```bash
# Create invoice with multiple custom fields
curl -X POST "http://localhost:5039/api/invoice/create" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "6",
    "invoiceDate": "2025-12-01",
    "dueDate": "2025-12-31",
    "lineItems": [
      {
        "itemId": "9",
        "description": "Consulting Services",
        "quantity": 2,
        "unitPrice": 150.00,
        "amount": 300.00
      },
      {
        "itemId": "8",
        "description": "Additional Support",
        "quantity": 1,
        "unitPrice": 75.00,
        "amount": 75.00
      }
    ],
    "customFields": [
      {
        "definitionId": "1000000010",
        "name": "Project Code",
        "value": "PRJ-2025-001",
        "dataType": "STRING"
      },
      {
        "definitionId": "1000000011",
        "name": "Amount Field",
        "value": "500",
        "dataType": "NUMBER"
      }
    ],
    "notes": "Net 30 payment terms"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "62",
    "docNumber": "1001",
    "totalAmt": 375.00,
    "customField": [
      {
        "definitionId": "1000000010",
        "name": "Project Code",
        "stringValue": "PRJ-2025-001"
      },
      {
        "definitionId": "1000000011",
        "name": "Amount Field",
        "numberValue": 500
      }
    ]
  }
}
```

---

#### **UPDATE - Invoice with Custom Fields**

Updates existing invoices with proper line item and custom field handling.

**Endpoint:** `PUT /api/invoice/update`

**Required Fields:**
- `invoiceId` (string): Invoice ID to update
- `syncToken` (string): Current SyncToken (for optimistic locking)
- All fields from CREATE (customerId, invoiceDate, lineItems, etc.)

**Line Item Update Rules:**
- Include `lineId` for existing line items to update them
- Omit `lineId` for new line items to add them
- Line items without `lineId` are treated as new additions

**Example:**
```bash
# Update invoice with multiple line items and custom fields
curl -X PUT "http://localhost:5039/api/invoice/update" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceId": "62",
    "syncToken": "14",
    "customerId": "6",
    "invoiceDate": "2025-12-01",
    "dueDate": "2025-12-31",
    "lineItems": [
      {
        "lineId": "16",
        "itemId": "9",
        "description": "Updated Consulting",
        "quantity": 3,
        "unitPrice": 150.00,
        "amount": 450.00
      },
      {
        "itemId": "8",
        "description": "New Additional Service",
        "quantity": 1,
        "unitPrice": 100.00,
        "amount": 100.00
      }
    ],
    "customFields": [
      {
        "definitionId": "1000000010",
        "name": "Project Code",
        "value": "PRJ-2025-002",
        "dataType": "STRING"
      }
    ],
    "notes": "Updated terms"
  }'
```

---

#### **READ - Invoice List**

Retrieves paginated invoice list with custom fields.

**Endpoint:** `GET /api/invoice/list?page={page}&pageSize={pageSize}`

**Example:**
```bash
# Get first 10 invoices
curl -X GET "http://localhost:5039/api/invoice/list?page=1&pageSize=10"

# Get next page
curl -X GET "http://localhost:5039/api/invoice/list?page=2&pageSize=10"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "invoices": [
      {
        "id": "62",
        "docNumber": "1001",
        "customerRef": {
          "value": "6",
          "name": "Acme Corp"
        },
        "txnDate": "2025-12-01",
        "dueDate": "2025-12-31",
        "totalAmt": 375.00,
        "balance": 375.00,
        "customField": [
          {
            "definitionId": "1000000010",
            "name": "Project Code",
            "stringValue": "PRJ-2025-001"
          }
        ],
        "line": [
          {
            "id": "16",
            "amount": 300.00,
            "description": "Consulting Services",
            "salesItemLineDetail": {
              "itemRef": { "value": "9" },
              "qty": 2,
              "unitPrice": 150.00
            }
          }
        ]
      }
    ],
    "page": 1,
    "pageSize": 10,
    "hasMore": false
  }
}
```

---

#### **Supporting Endpoints**

**Get Customers:**
```bash
curl -X GET "http://localhost:5039/api/customer/list"
```

**Get Items/Services:**
```bash
curl -X GET "http://localhost:5039/api/item/list"
```

---

### **Custom Fields Integration with Invoices**

#### **How Custom Fields Work with Invoices**

1. **Create Custom Field for Invoices** (App Foundations GraphQL):
```bash
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Project Code",
    "type": "STRING",
    "associations": [{
      "associatedEntity": "/transactions/Transaction",
      "active": true,
      "required": false,
      "associationCondition": "INCLUDED",
      "subAssociations": ["SALE_INVOICE"]
    }]
  }'
```

**Response includes `legacyIDV2`:**
```json
{
  "success": true,
  "data": {
    "id": "udcf_1000000010",
    "legacyIDV2": "1000000010",
    "label": "Project Code",
    "dataType": "STRING"
  }
}
```

2. **Use `legacyIDV2` in Invoice** (REST API v3):
```bash
curl -X POST "http://localhost:5039/api/invoice/create" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "6",
    "invoiceDate": "2025-12-01",
    "lineItems": [...],
    "customFields": [
      {
        "definitionId": "1000000010",
        "name": "Project Code",
        "value": "PRJ-2025-001",
        "dataType": "STRING"
      }
    ]
  }'
```

#### **Custom Field Payload Structure**

The API request uses a `customFields` array with the following structure:
```json
{
  "customFields": [
    {
      "definitionId": "1000000010",
      "name": "Project Code",
      "value": "PRJ-2025-001",
      "dataType": "STRING"
    },
    {
      "definitionId": "1000000011",
      "name": "Amount",
      "value": "500",
      "dataType": "NUMBER"
    },
    {
      "definitionId": "1000000012",
      "name": "Due Date",
      "value": "2025-12-31",
      "dataType": "DATE"
    }
  ]
}
```

- **definitionId**: Use `legacyIDV2` from custom field definition
- **name**: Custom field label/name
- **value**: The actual custom field value (always sent as string)
- **dataType**: Data type (STRING, NUMBER, DATE)

**Note**: All values are sent as `StringValue` to the QuickBooks API. QuickBooks returns them in the appropriate format (`StringValue`, `NumberValue`, or `DateValue`) based on the field's data type.

---

### **Error Handling & Validation**

#### **HTTP Status Codes**
- `200 OK` - Successful operation
- `400 Bad Request` - Invalid request data, GraphQL errors, or QuickBooks validation errors
- `401 Unauthorized` - Missing or invalid OAuth token
- `404 Not Found` - Custom field or invoice not found
- `500 Internal Server Error` - Server-side errors

#### **Common Invoice Errors**

**Invalid ItemRef:**
```json
{
  "error": "Invalid Reference Id : Line.SalesItemLineDetail.ItemRef"
}
```
**Solution**: Ensure `itemId` exists in QuickBooks item list

**Missing Line.Id for Updates:**
```
"Only first line item updates, others are replaced"
```
**Solution**: Include `lineId` from original invoice for each existing line item

**Custom Field Not Updating:**
```json
{
  "error": "CustomField update failed"
}
```
**Solution**: Ensure all required fields (`definitionId`, `name`, `value`, `dataType`) are included in each custom field object

**SyncToken Mismatch:**
```json
{
  "error": "Stale SyncToken"
}
```
**Solution**: Fetch latest invoice data to get current `syncToken`

#### **Best Practices**
- Always check OAuth token validity before operations
- Use `GET /api/customfields/summaries` to get `legacyIDV2` for invoice custom fields
- Include `lineId` when updating existing line items
- Include all required fields (`definitionId`, `name`, `value`, `dataType`) for each custom field
- Test association combinations in development before production
- Monitor for GraphQL-specific error messages for debugging
- Use soft delete for production safety

## Web UI Features

The application includes a modern web interface for managing both custom fields and invoices.

### **Access the UI**
Navigate to `http://localhost:5039` in your browser to access the interactive web interface.

### **UI Components**

#### **Dashboard**
- OAuth authentication status
- Quick access to custom fields and invoices
- Real-time connection status with QuickBooks

#### **Custom Fields Management**
- **List View**: Display all custom field definitions with filtering
- **Create/Edit Forms**: Interactive forms for managing custom fields
- **Association Builder**: Visual interface for entity associations
- **Type Selection**: Dropdown for data types (STRING, NUMBER, DATE)
- **Real-time Validation**: Client-side validation before submission

#### **Invoice Management**
- **Invoice List**: Paginated table showing all invoices
  - Displays: Invoice #, Customer, Date, Due Date, Total, Balance, Custom Fields
  - Edit and view capabilities for each invoice
  - Pagination controls for large datasets

- **Create/Edit Invoice Modal**:
  - **Customer Selection**: Dropdown populated from QuickBooks customers
  - **Date Fields**: Invoice date and due date pickers
  - **Line Items**:
    - Item/Service dropdown with auto-populated unit prices
    - Description field for additional details
    - Quantity and unit price inputs
    - Real-time amount calculation
    - Add/remove multiple line items
    - **Invoice Total**: Auto-calculated sum of all line items
  - **Custom Fields**:
    - All SALE_INVOICE associated custom fields displayed automatically
    - Dynamic input types based on field data type (text, number, date picker)
    - Pre-populated on edit with existing values
    - Support for multiple custom fields per invoice
  - **Notes**: Optional memo field

#### **Features**
- **Real-time Calculations**: Line item amounts and invoice totals update automatically
- **Data Persistence**: All changes sync with QuickBooks immediately
- **Error Handling**: User-friendly error messages for validation issues
- **Responsive Design**: Works on desktop and mobile devices
- **Bootstrap 5**: Modern, clean interface with icons

### **UI Workflow Example**

1. **Authenticate**: Click "Connect to QuickBooks" button
2. **Create Custom Field**:
   - Navigate to Custom Fields tab
   - Click "Create Custom Field"
   - Enter name, select type, configure associations
   - Save to QuickBooks
3. **Edit Custom Field**:
   - Click "Edit" button on a custom field row
   - Modify name, status, or associations
   - Save changes to QuickBooks
4. **Create Invoice**:
   - Navigate to Invoices tab
   - Click "Create Invoice"
   - Select customer from dropdown
   - Add line items by selecting items/services
   - Fill in values for any available custom fields (STRING, NUMBER, DATE)
   - Click "Create Invoice"
5. **Edit Invoice**:
   - Click "Edit" button on invoice row
   - Modify line items, dates, or custom field values
   - All custom fields display with their current values pre-filled
   - Click "Update Invoice"

## API Testing

Use Swagger UI or the Web UI for testing all API operations:

### **Swagger UI**
- **Interactive Documentation**: Explore all available endpoints
- **OAuth Flow**: Test the complete authentication flow with App Foundations scopes
- **Custom Field Definition Operations**: Create, read, update, delete custom field definitions
- **Association Management**: Test complex entity associations and sub-associations
- **Invoice Operations**: Test invoice CRUD with custom fields
- **Legacy Compatibility**: Test both new and legacy endpoint formats
- **Real-time Responses**: View API responses and error details
- **Request Validation**: Built-in parameter validation and examples

### **Web UI Testing**
- **Visual Interface**: Test all operations through the web interface
- **Real-time Validation**: See validation errors immediately
- **Live Data**: Work with actual QuickBooks data
- **User Experience**: Test the complete user workflow

## Development

### Project Structure
```
QuickBooks-CustomFields-API/
├── Controllers/
│   ├── CustomFieldsController.cs    # Custom field CRUD operations
│   ├── InvoiceController.cs         # Invoice management
│   ├── CustomerController.cs        # Customer list endpoint
│   ├── ItemController.cs            # Items/services endpoint
│   └── OAuthController.cs           # OAuth authentication
├── Models/
│   ├── SharedModels.cs              # Shared API models
│   ├── CustomFields.cs              # Custom field models
│   └── Invoice.cs                   # Invoice and line item models
├── Services/
│   ├── ICustomFieldService.cs       # Custom field service interface
│   ├── CustomFieldService.cs        # GraphQL custom field operations
│   ├── ITokenManagerService.cs      # Token management interface
│   └── TokenManagerService.cs       # OAuth token handling
├── wwwroot/
│   ├── index.html                   # Main web UI
│   ├── js/
│   │   └── app.js                   # Frontend application logic
│   └── css/
│       └── styles.css               # Custom styles
├── Program.cs                       # Application startup
└── appsettings.json                 # Configuration
```

### NuGet Packages
- `IppDotNetSdkForQuickBooksApiV3` (v14.7.0) - QuickBooks SDK for OAuth
- `GraphQL.Client` (v6.0.2) - GraphQL client for App Foundations API
- `GraphQL.Client.Serializer.Newtonsoft` (v6.0.2) - JSON serialization

### Key Features
- **App Foundations Integration**: Native support for the latest QuickBooks API
- **Smart Legacy Conversion**: Automatic conversion between API formats
- **Association Management**: Handle complex entity relationships
- **Dual ID Support**: Handle both legacy encoded and V2 numeric IDs
- **Extension Methods**: Rich helper methods for data manipulation

## Error Handling

The API includes comprehensive error handling:
- **App Foundations GraphQL**: Advanced error parsing and reporting
- **OAuth Error Handling**: User-friendly messages for authentication issues
- **Association Validation**: Errors for invalid entity associations
- **Legacy Conversion**: Graceful handling of data format differences
- **Network and Timeout**: Robust error handling for API calls

## Security

- **OAuth 2.0 with App Foundations Scopes**: Granular permission control
- **State Parameter Validation**: CSRF protection for OAuth flows
- **Secure Token Storage**: Automatic refresh with proper scope management
- **Session-based State Management**: Secure OAuth state handling
- **Environment Isolation**: Separate sandbox/production configurations

## Testing

### **Complete CRUD Testing Guide**

#### **1. Authentication Setup**
```bash
# Get OAuth authorization URL
curl -X GET "http://localhost:5039/api/oauth/authorize"

# Open returned URL in browser to authorize with QuickBooks
# Check authorization status
curl -X GET "http://localhost:5039/api/oauth/status"
```

#### **2. CREATE Testing**
```bash
# Test default association
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Field", "type": "STRING"}'

# Test custom associations
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Contact Field",
    "type": "STRING",
    "associations": [{
      "associatedEntity": "/network/Contact",
      "subAssociations": ["VENDOR"]
    }]
  }'

# Test association business rules (should fail)
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mixed Entity Field",
    "type": "STRING",
    "associations": [
      {"associatedEntity": "/network/Contact", "subAssociations": ["CUSTOMER"]},
      {"associatedEntity": "/transactions/Transaction", "subAssociations": ["SALE_INVOICE"]}
    ]
  }'
```

#### **3. READ Testing**
```bash
# Get all definitions
curl -X GET "http://localhost:5039/api/customfields/definitions"

# Get summaries with associations
curl -X GET "http://localhost:5039/api/customfields/summaries" | jq '.data[] | {name, active, associatedEntities}'

# Get specific field
curl -X GET "http://localhost:5039/api/customfields/definitions/{id}"

# Legacy compatibility
curl -X GET "http://localhost:5039/api/customfields"
```

#### **4. UPDATE Testing**
```bash
# Simple name update
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Updated Name"}'

# Deactivate field
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{"active": false}'

# Update associations
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{
    "associations": [{
      "associatedEntity": "/network/Contact",
      "subAssociations": ["CUSTOMER"]
    }]
  }'

# Test invalid data type (should fail)
curl -X PUT "http://localhost:5039/api/customfields/{id}" \
  -H "Content-Type: application/json" \
  -d '{"type": "INVALID_TYPE"}'
```

#### **5. DELETE Testing**
```bash
# Soft delete (deactivate)
curl -X DELETE "http://localhost:5039/api/customfields/{id}"

# Verify deletion
curl -X GET "http://localhost:5039/api/customfields/summaries" | jq '.data[] | select(.id == "{id}") | .active'

# Test delete non-existent field (should fail)
curl -X DELETE "http://localhost:5039/api/customfields/invalid_id"
```

#### **6. Invoice Testing**

**Test Invoice Creation:**
```bash
# Get customers and items first
curl -X GET "http://localhost:5039/api/customer/list"
curl -X GET "http://localhost:5039/api/item/list"

# Create invoice with custom field
curl -X POST "http://localhost:5039/api/invoice/create" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "6",
    "invoiceDate": "2025-12-16",
    "dueDate": "2026-01-15",
    "lineItems": [
      {
        "itemId": "9",
        "description": "Web Development",
        "quantity": 10,
        "unitPrice": 100.00,
        "amount": 1000.00
      }
    ],
    "customFieldLegacyId": "1000000010",
    "customFieldName": "Project Code",
    "customFieldValue": "WEB-2025-001"
  }'
```

**Test Invoice Update:**
```bash
# Get invoice list first to get ID and SyncToken
curl -X GET "http://localhost:5039/api/invoice/list?page=1&pageSize=10"

# Update invoice (use ID and SyncToken from above)
curl -X PUT "http://localhost:5039/api/invoice/update" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceId": "62",
    "syncToken": "0",
    "customerId": "6",
    "invoiceDate": "2025-12-16",
    "dueDate": "2026-01-15",
    "lineItems": [
      {
        "lineId": "1",
        "itemId": "9",
        "description": "Updated Description",
        "quantity": 15,
        "unitPrice": 100.00,
        "amount": 1500.00
      },
      {
        "itemId": "8",
        "description": "Additional Service",
        "quantity": 5,
        "unitPrice": 50.00,
        "amount": 250.00
      }
    ],
    "customFieldLegacyId": "1000000010",
    "customFieldName": "Project Code",
    "customFieldValue": "WEB-2025-002"
  }'
```

**Test Invoice with Custom Field Integration:**
```bash
# 1. Create custom field for invoices
curl -X POST "http://localhost:5039/api/customfields" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Department",
    "type": "STRING",
    "associations": [{
      "associatedEntity": "/transactions/Transaction",
      "active": true,
      "subAssociations": ["SALE_INVOICE"]
    }]
  }'

# 2. Get legacyIDV2 from response (e.g., "1000000011")

# 3. Create invoice with new custom field
curl -X POST "http://localhost:5039/api/invoice/create" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "6",
    "invoiceDate": "2025-12-16",
    "lineItems": [{
      "itemId": "9",
      "description": "Service",
      "quantity": 1,
      "unitPrice": 500.00,
      "amount": 500.00
    }],
    "customFieldLegacyId": "1000000011",
    "customFieldName": "Department",
    "customFieldValue": "Engineering"
  }'

# 4. Verify custom field in invoice list
curl -X GET "http://localhost:5039/api/invoice/list?page=1&pageSize=10" | \
  jq '.data.invoices[] | select(.customField != null) | {id, customField}'
```

#### **7. Swagger UI Testing**
1. Navigate to `http://localhost:5039` (redirects to Swagger)
2. Use `/api/oauth/authorize` to initiate QuickBooks connection
3. Test all CRUD endpoints interactively:
   - **Custom Fields**:
     - **POST** `/api/customfields` - Create with associations
     - **GET** `/api/customfields/definitions` - Read with full data
     - **PUT** `/api/customfields/{id}` - Update operations
     - **DELETE** `/api/customfields/{id}` - Soft delete
   - **Invoices**:
     - **GET** `/api/invoice/list` - List invoices with pagination
     - **POST** `/api/invoice/create` - Create with line items and custom fields
     - **PUT** `/api/invoice/update` - Update existing invoices
   - **Supporting**:
     - **GET** `/api/customer/list` - Get customers for invoice creation
     - **GET** `/api/item/list` - Get items/services for line items
4. Explore App Foundations endpoints vs REST API v3
5. View detailed request/response examples with association data

#### **8. Web UI Testing**
1. Navigate to `http://localhost:5039` in your browser
2. Click "Connect to QuickBooks" to authenticate
3. Test Custom Fields:
   - Create a new custom field for SALE_INVOICE
   - Verify it appears in the list
   - Edit and update the field
4. Test Invoices:
   - Click "Create Invoice" button
   - Select customer, add line items, add custom field value
   - Save and verify in invoice list
   - Click "Edit" on an invoice
   - Modify line items and custom fields
   - Update and verify changes
5. Test Multi-Line Items:
   - Create invoice with 3+ line items
   - Edit and modify multiple line items
   - Verify all line items update correctly
6. Test Custom Field Integration:
   - Create invoice with custom field
   - Edit invoice and change custom field value
   - Verify custom field updates in QuickBooks

## Support

For issues or questions:
1. **Check API Responses**: Use Swagger UI to inspect detailed responses
2. **Verify App Configuration**: Ensure `appsettings.json` has correct App Foundations scopes
3. **OAuth Setup**: Confirm app has `app-foundations.custom-field-definitions` permissions
4. **Review Logs**: Check application logs for detailed error messages
5. **Test Authentication**: Use `/api/oauth/status` to verify token and scopes
6. **Association Issues**: Check entity associations in `/api/customfields/summaries`
7. **Legacy Compatibility**: Compare responses between new and legacy endpoints

