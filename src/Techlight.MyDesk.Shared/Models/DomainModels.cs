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
    public string InvoiceNum { get; set; } = "";          // customer-facing invoice number
    public DateTime InvoiceDate { get; set; }
    public string Code { get; set; } = "";               // FK Users — Invoiced By
    public string InvoicedBy { get; set; } = "";         // display name from Users
    public int InvoiceStatusId { get; set; }
    public string StatusName { get; set; } = "";
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = "";
    public int Qid { get; set; }                         // FK Quotes (0 = standalone)
    public int CompanyId { get; set; }                   // FK Companies (142 = not an account)
    public int? ContactId { get; set; }
    public string CCompany { get; set; } = "";           // denormalised company name for display
    public string InvCompany { get; set; } = "";
    public string DelCompany { get; set; } = "";
    public string InvAddress { get; set; } = "";
    public string DelAddress { get; set; } = "";
    public string? CustomerPO { get; set; }
    public string? Attention { get; set; }
    public string? Account { get; set; }
    public string? Terms { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public decimal NettPriceTotal { get; set; }          // ex-GST total
    public decimal GSTTotal { get; set; }
    public decimal TotalIncGST => NettPriceTotal + GSTTotal;
    public bool ExportedToMYOB { get; set; }
    public DateTime? ExportedDate { get; set; }
}

public class InvoiceLineItem
{
    public int InvoiceContentId { get; set; }
    public int InvoiceId { get; set; }
    public decimal Quantity { get; set; }
    public string? ProductCode { get; set; }
    public string Description { get; set; } = "";
    public decimal NettPrice { get; set; }               // unit price
    public decimal ExtNettPrice { get; set; }            // extended = qty * unit
}

public class InvoiceAuditEntry
{
    public string? Code { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public DateTime DateEntered { get; set; }
}

public class DespatchDetail
{
    public int DespatchId { get; set; }
    public int InvoiceId { get; set; }
    public DateTime DespatchDate { get; set; } = DateTime.Today;
    public string? Carrier { get; set; }
    public string? CarrierRef { get; set; }
    public string? PackageDetails { get; set; }
    public string? InternalNotes { get; set; }
}

public class InvoiceReportRequest
{
    public string? CustomerName { get; set; }
    public string? OriginatorCode { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? Qid { get; set; }
    public string? Status { get; set; }
}

// ============================================================================
// Purchase Order Models
// ============================================================================
public class PurchaseOrder
{
    public int POid { get; set; }                        // PK
    public string Code { get; set; } = "";               // FK Users — originator
    public string OriginatorName { get; set; } = "";
    public string Project { get; set; } = "";            // project / job / replacement (required)
    public int ContactId { get; set; }                   // FK Contacts — supplier
    public string SupplierName { get; set; } = "";
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = "";
    public DateTime PODate { get; set; }
    public int POStatusId { get; set; }
    public string StatusName { get; set; } = "";
    public bool GST { get; set; } = true;
    public int? POPaymentTypeId { get; set; }
    public string? PaymentType { get; set; }
    public string? Terms { get; set; }
    public DateTime DateRequired { get; set; }
    public int? DeliverToLocationId { get; set; }
    public string? LocationName { get; set; }
    public string? DeliverToLocation { get; set; }       // free-text delivery address
    public string? IntroText { get; set; }               // notes visible to supplier
    public string? InternalNotes { get; set; }           // reason for purchase
    public decimal PriceExTotal { get; set; }
    public decimal PriceGSTTotal { get; set; }
    public decimal PriceIncTotal { get; set; }
    public int RFQid { get; set; }
    public int Qid { get; set; }
    public bool HasCapEx { get; set; }
}

public class POLineItem
{
    public int POItemId { get; set; }
    public int POid { get; set; }
    public int? PartCodeId { get; set; }
    public int Quantity { get; set; } = 1;
    public string Description { get; set; } = "";
    public decimal PriceEx { get; set; }                 // unit price
    public decimal PriceExSubTotal { get; set; }         // extended
    public int? POProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public bool IsCapEx { get; set; }
}

public class POAuditEntry
{
    public string? Code { get; set; }
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public DateTime DateEntered { get; set; }
}

public class POProductType
{
    public int POProductTypeId { get; set; }
    public string POProductTypeName { get; set; } = "";
    public bool IsCapEx { get; set; }
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

public class UserLookup
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
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
    public int ThisMonthPOs { get; set; }
    public decimal ThisMonthPOValue { get; set; }
    public int LastMonthPOs { get; set; }
    public int ThisMonthDespatch { get; set; }
    public decimal[] MonthlyPOsThisYear { get; set; } = new decimal[12];
    public List<ActivityFeedItem> RecentActivity { get; set; } = new();
}

public class ActivityFeedItem
{
    public string UserCode   { get; set; } = "";
    public string UserName   { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int?   EntityId   { get; set; }
    public string EntityRef  { get; set; } = "";
    public string Action     { get; set; } = "";
    public DateTime ActivityDate { get; set; }

    public string TimeAgo
    {
        get
        {
            var d = DateTime.Now - ActivityDate;
            if (d.TotalMinutes < 1)  return "just now";
            if (d.TotalHours   < 1)  return $"{(int)d.TotalMinutes}m ago";
            if (d.TotalHours   < 24) return $"{(int)d.TotalHours}h ago";
            if (d.TotalDays    < 7)  return $"{(int)d.TotalDays}d ago";
            return ActivityDate.ToString("dd/MM/yyyy");
        }
    }

    public string NavUrl => EntityType switch
    {
        "Quote"    => EntityId.HasValue ? $"/quotes/{EntityId}"          : "/quotes",
        "Invoice"  => EntityId.HasValue ? $"/invoices/{EntityId}"        : "/invoices",
        "PO"       => EntityId.HasValue ? $"/purchase-orders/{EntityId}" : "/purchase-orders",
        "Despatch" => "/despatch",
        _          => "/",
    };
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
