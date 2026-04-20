namespace Techlight.MyDesk.Shared.Models;

// ============================================================================
// Quote Models
// ============================================================================
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
    public string? Attention { get; set; }
    public string? Delivery { get; set; }
    public int Validity { get; set; } = 30;
    public string? QuoteNumber { get; set; }
    public string? SenderCode { get; set; }
    public string? ContactName { get; set; }
    public string? DivisionName { get; set; }
}

public class QuoteLineItem
{
    public int QuoteItemId { get; set; }
    public int Qid { get; set; }
    public string? ProductCode { get; set; }
    public string? Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Units { get; set; }
    public decimal Days { get; set; }
    public decimal UnitCost { get; set; }
    public decimal MinNettPrice { get; set; }
    public decimal NettPrice { get; set; }
    public decimal UnitCostSubTotal { get; set; }
    public decimal ExtNettPrice { get; set; }
    public decimal EffectiveQty => Days > 0 && Units > 0 ? Units * Days : Quantity;
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
    public decimal NettPrice { get; set; }
}

public class QuoteThirdPartyItem
{
    public int QuoteThirdPartyId { get; set; }
    public int QuoteId { get; set; }
    public string? Description { get; set; }
    public string? Supplier { get; set; }
    public string? SupplierPartNumber { get; set; }
    public string? OurPartNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? Type { get; set; }
    public decimal UnitCost { get; set; }
    public decimal NettPrice { get; set; }
    public decimal ExtNettPrice { get; set; }
    public decimal TotalCost { get; set; }
}

public class QuoteAuditEntry
{
    public int Qid { get; set; }
    public string? Code { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public DateTime DateEntered { get; set; }
}

// ============================================================================
// Invoice Models
// ============================================================================
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
    public int InvoiceStatusId { get; set; }
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

// ============================================================================
// Purchase Order Models
// ============================================================================
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
    public int POStatusId { get; set; }
}

// ============================================================================
// Contact & Company Models
// ============================================================================
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
    public string FullName => $"{FirstName} {Surname}".Trim();
}

public class Company
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Suburb { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? ABN { get; set; }
}

// ============================================================================
// User & Auth Models
// ============================================================================
public class User
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public int? DivisionId { get; set; }
    public string? UserRole { get; set; }
    public int UserTypeId { get; set; }
}

public class UserRole
{
    public int UserRoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

// ============================================================================
// Lookup / Reference Data Models
// ============================================================================
public class Division
{
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
}

public class Location
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
}

public class ActivityType
{
    public int ActivityTypeId { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
}

public class PartCode
{
    public int PartCodeId { get; set; }
    public string PartCodeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class QuoteStatus
{
    public int QuoteStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class InvoiceStatus
{
    public int InvoiceStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class POStatus
{
    public int POStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

// ============================================================================
// Dashboard Models
// ============================================================================
public class DashboardMetrics
{
    public int ThisMonthQuotes { get; set; }
    public int ThisMonthQuotesWon { get; set; }
    public decimal ThisMonthQuotesValue { get; set; }
    public int LastMonthQuotesWon { get; set; }
    public int ThisMonthInvoices { get; set; }
    public decimal ThisMonthInvoiceValue { get; set; }
    public int LastMonthInvoices { get; set; }
    public int YtdQuotesWon { get; set; }
    public decimal YtdQuotesValue { get; set; }
    public int YtdInvoices { get; set; }
    public decimal YtdInvoiceValue { get; set; }
    public decimal LastYearYtdQuotesValue { get; set; }
    public int PendingQuotesOver30Days { get; set; }
    public int InvoicesOverdue { get; set; }
    public int PendingApprovalPOs { get; set; }
    public decimal[] MonthlyQuotesThisYear { get; set; } = new decimal[12];
    public decimal[] MonthlyQuotesLastYear { get; set; } = new decimal[12];
    public decimal[] MonthlyInvoicesThisYear { get; set; } = new decimal[12];
}

// ============================================================================
// Report Models
// ============================================================================
public class ReportResult
{
    public string Title { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<Dictionary<string, object>> Records { get; set; } = new();
    public string? SummaryText { get; set; }
}
