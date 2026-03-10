using System.Text.Json.Serialization;

namespace QuickBooks_CustomFields_API.Models
{
    public class Invoice
    {
        public string? Id { get; set; }
        public string? SyncToken { get; set; }
        public string? DocNumber { get; set; }
        public string? TxnDate { get; set; }
        public string? DueDate { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal Balance { get; set; }
        public CustomerRef? CustomerRef { get; set; }
        public List<InvoiceLine>? Line { get; set; }
        public List<CustomFieldValue>? CustomField { get; set; }
        public CustomerMemo? CustomerMemo { get; set; }
    }

    public class CustomerRef
    {
        public string? Value { get; set; }
        public string? Name { get; set; }
    }

    public class InvoiceLine
    {
        public string? Id { get; set; }
        public decimal Amount { get; set; }
        public string? DetailType { get; set; }
        public string? Description { get; set; }
        public SalesItemLineDetail? SalesItemLineDetail { get; set; }
    }

    public class SalesItemLineDetail
    {
        public ItemRef? ItemRef { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ItemRef
    {
        public string? Value { get; set; }
        public string? Name { get; set; }
    }

    public class CustomFieldValue
    {
        public string? DefinitionId { get; set; }
        public string? StringValue { get; set; }
        public object? NumberValue { get; set; }
        public string? DateValue { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
    }

    public class CustomerMemo
    {
        public string? Value { get; set; }
    }

    public class Customer
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? CompanyName { get; set; }
        public bool Active { get; set; }
    }

    public class QuickBooksInvoiceQueryResponse
    {
        public InvoiceQueryResponse? QueryResponse { get; set; }
    }

    public class InvoiceQueryResponse
    {
        public List<Invoice>? Invoice { get; set; }
        public int MaxResults { get; set; }
        public int StartPosition { get; set; }
    }

    public class QuickBooksCustomerQueryResponse
    {
        public CustomerQueryResponse? QueryResponse { get; set; }
    }

    public class CustomerQueryResponse
    {
        public List<Customer>? Customer { get; set; }
        public int MaxResults { get; set; }
        public int StartPosition { get; set; }
    }

    public class QuickBooksInvoiceResponse
    {
        public Invoice? Invoice { get; set; }
        public string? Time { get; set; }
    }

    public class InvoiceCreateRequest
    {
        public string? InvoiceId { get; set; }
        public string? SyncToken { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string? DueDate { get; set; }
        public List<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
        public List<InvoiceCustomField>? CustomFields { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceCustomField
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = "STRING";
    }

    public class InvoiceLineItem
    {
        public string? LineId { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class Item
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public decimal UnitPrice { get; set; }
        public bool Active { get; set; }
    }

    public class QuickBooksItemQueryResponse
    {
        public ItemQueryResponse? QueryResponse { get; set; }
    }

    public class ItemQueryResponse
    {
        public List<Item>? Item { get; set; }
        public int MaxResults { get; set; }
    }
}
