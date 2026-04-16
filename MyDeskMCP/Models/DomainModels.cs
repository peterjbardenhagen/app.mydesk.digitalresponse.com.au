namespace Techlight.MyDesk.MCP.Models;

// Quote Models
public class Quote
{
    public int Qid { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Project { get; set; }
    public string QuoteStatus { get; set; } = string.Empty;
    public decimal UnitCostTotal { get; set; }
    public decimal NettPriceTotal { get; set; }
    public decimal Margin { get; set; }
    public DateTime QuoteDate { get; set; }
    public string Originator { get; set; } = string.Empty;
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? Terms { get; set; }
    public int ContactId { get; set; }
    public int DivisionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int QuoteStatusId { get; set; }
}

public class QuoteLineItem
{
    public int QuoteContentsId { get; set; }
    public int Qid { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Units { get; set; }
    public decimal Days { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateQuoteRequest
{
    public int ContactId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Project { get; set; }
    public int DivisionId { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? Terms { get; set; }
    public List<QuoteLineItemRequest> LineItems { get; set; } = new();
}

public class QuoteLineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal? Units { get; set; }
    public decimal? Days { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
}

// Invoice Models
public class Invoice
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountExGST { get; set; }
    public decimal GST { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Originator { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public int? QuoteId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int ContactId { get; set; }
}

public class InvoiceReportRequest
{
    public string? CustomerName { get; set; }
    public string? OriginatorCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? QuoteId { get; set; }
    public string? Status { get; set; }
}

// Purchase Order Models
public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountExGST { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PODate { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public string Originator { get; set; } = string.Empty;
    public int? QuoteId { get; set; }
    public int ContactId { get; set; }
}

public class UpdatePOStatusRequest
{
    public int PurchaseOrderId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// Contact Models
public class Contact
{
    public int ContactId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Suburb { get; set; }
    public string? PostCode { get; set; }
    public string? CustomerCode { get; set; }
    public string? SupplierCode { get; set; }
    public string Originator { get; set; } = string.Empty;
    public int CompanyId { get; set; }
}

// User Models
public class User
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public int? DivisionId { get; set; }
    public string? UserRole { get; set; }
}

// Email Request
public class EmailQuoteRequest
{
    public int QuoteId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public bool IncludePDF { get; set; } = true;
}

// Report Results
public class ReportResult
{
    public string Title { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<Dictionary<string, object>> Records { get; set; } = new();
    public string? SummaryText { get; set; }
}

// MCP Context for authentication
public class McpContext
{
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<int> AccessibleDivisions { get; set; } = new();
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
}
