namespace MyDesk.Shared.Models;

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
    public int CompanyId { get; set; }
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

    public DateTime ExpiryDate => QuoteDate.AddDays(Validity);
    public bool IsExpired => DateTime.Today > ExpiryDate;
    public bool IsExpiringSoon => !IsExpired && DateTime.Today.AddDays(7) >= ExpiryDate;
}

// ============================================================================
// Log Viewer Models
// ============================================================================
public enum LogType
{
    Application,
    Error,
    All
}

public enum LogLevel
{
    All,
    Error,
    Warning,
    Info
}

public enum PurgeOption
{
    OlderThan30Days,
    OlderThan7Days,
    ErrorLogsOnly,
    AllLogs
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
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
    public decimal NettPriceTotal { get; set; }         // ex-GST total (matches SQL column 'NettPriceTotal')
    public decimal GSTTotal { get; set; }
    public decimal TotalIncGST => NettPriceTotal + GSTTotal;
    public bool ExportedToMYOB { get; set; }
    public DateTime? ExportedDate { get; set; }
    // UI-only properties for selection and email
    public bool IsSelected { get; set; }
    public string? Email { get; set; }
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

public class POInvoiceEntry
{
    public int PurchaseOrderInvoiceId { get; set; }
    public int POid { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime? InvoiceDate { get; set; }
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
    public string? CustomerCode { get; set; }
    public string? SupplierCode { get; set; }
    public string Originator { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string FullName => $"{FirstName} {Surname}".Trim();
    
    // Customer Portal Credentials
    public string? PortalUsername { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool IsPortalEnabled { get; set; }
    public DateTime? PortalLastLogin { get; set; }
    public DateTime? PortalAccessExpires { get; set; }
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
    public string? CustomerCode { get; set; }
    public string? SupplierCode { get; set; }
    public bool IsCustomer { get; set; } = true;
    public bool IsSupplier { get; set; }
    public string? Notes { get; set; }
    public string? Country { get; set; } = "Australia";
    public bool HasGST => !string.IsNullOrWhiteSpace(ABN) && ABN.Length >= 11 && (string.IsNullOrWhiteSpace(Country) || Country.Equals("Australia", StringComparison.OrdinalIgnoreCase));
    public string? DefaultTerms { get; set; }
    public string? PaymentTerms { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? InvAddress1 { get; set; }
    public string? InvAddress2 { get; set; }
    public string? InvSuburb { get; set; }
    public string? InvState { get; set; }
    public string? InvPostCode { get; set; }
    public string? DelAddress1 { get; set; }
    public string? DelAddress2 { get; set; }
    public string? DelSuburb { get; set; }
    public string? DelState { get; set; }
    public string? DelPostCode { get; set; }
    public string FullAddress => FormatAddress(Address1, Address2, Suburb, State, PostCode);
    public string InvoiceAddress => FormatAddress(InvAddress1, InvAddress2, InvSuburb, InvState, InvPostCode);
    public string DeliveryAddress => FormatAddress(DelAddress1, DelAddress2, DelSuburb, DelState, DelPostCode);

    private static string FormatAddress(string? a1, string? a2, string? sub, string? st, string? pc)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a1)) parts.Add(a1);
        if (!string.IsNullOrWhiteSpace(a2)) parts.Add(a2);
        if (!string.IsNullOrWhiteSpace(sub)) parts.Add(sub);
        if (!string.IsNullOrWhiteSpace(st)) parts.Add(st);
        if (!string.IsNullOrWhiteSpace(pc)) parts.Add(pc);
        return string.Join(", ", parts);
    }
}

public class CompanyImportItem
{
    public string CompanyName { get; set; } = string.Empty;
    public string? InvAddress { get; set; }
    public string? DelAddress { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalValue { get; set; }
    public bool Selected { get; set; } = true;
    public int? ExistingCompanyId { get; set; }
    public string? ExistingCompanyName { get; set; }
    public bool IsDuplicate => ExistingCompanyId.HasValue;
}

public class ContactNote
{
    public int NoteId { get; set; }
    public int ContactId { get; set; }
    public DateTime Date { get; set; }
    public string NoteType { get; set; } = "";
    public string NoteText { get; set; } = "";
    public string CreatedBy { get; set; } = "";
}

public enum RoleType
{
    Director = 1,
    Administrator = 2,
    Accounts = 3,
    Sales = 4
}

public class User
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Password { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public int? DivisionId { get; set; }
    public string? UserRole { get; set; }
    public int UserTypeId { get; set; }
    public RoleType Role { get; set; } = RoleType.Administrator;
    public bool Active { get; set; } = true;
}

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserRole
{
    public int UserRoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class ReportResult
{
    public string Title { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public decimal? TotalAmount { get; set; }
    public decimal? TotalGrossProfit { get; set; }
    public decimal? AvgGrossProfitMargin { get; set; }
    public DateTime GeneratedAt { get; set; }
}

// ============================================================================
// Lookup / Reference Data Models
// ============================================================================
public class Division
{
    public int DivisionId { get; set; }
    public Guid TenantId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public decimal GSTRate { get; set; } = 10.0m;
    public string InvoicePrefix { get; set; } = "INV-";
    public string QuotePrefix { get; set; } = "QT-";
    public string POPrefix { get; set; } = "PO-";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Location
{
    public int LocationId { get; set; }
    public Guid TenantId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Suburb { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    
    public string FullAddress => string.Join(", ", new[] { Address1, Address2, Suburb, State, PostCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
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
    public string ProductCode { get; set; } = string.Empty;
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
    // ── Core Monthly Metrics ───────────────────────────────────────────────────
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

    // ── Advanced Business KPIs ─────────────────────────────────────────────────
    public decimal GrossProfitMargin { get; set; }
    public decimal AverageQuoteValue { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public decimal QuoteToInvoiceConversionRate { get; set; }
    public decimal YtdRevenueGrowth { get; set; }
    public decimal MonthOverMonthGrowth { get; set; }
    public decimal YearOverYearGrowth { get; set; }
    
    // ── Pipeline & Forecasting ─────────────────────────────────────────────────
    public decimal PipelineValue { get; set; }
    public int OpenQuotesCount { get; set; }
    public decimal ProjectedMonthlyRevenue { get; set; }
    public decimal QuarterlyTarget { get; set; }
    public decimal QuarterlyProgress { get; set; }
    
    // ── Team Performance ─────────────────────────────────────────────────────────
    public List<UserKPI> TeamMemberKPIs { get; set; } = new();
    public List<DivisionPerformance> DivisionPerformance { get; set; } = new();
    
    // ── Health Indicators ──────────────────────────────────────────────────────
    public List<BusinessWarning> Warnings { get; set; } = new();
    public List<BusinessRecommendation> Recommendations { get; set; } = new();
    public int OverallHealthScore { get; set; }
    
    // ── Comparative Analytics ────────────────────────────────────────────────────
    public decimal[] LastYearMonthlyRevenue { get; set; } = new decimal[12];
    public decimal[] TargetMonthlyRevenue { get; set; } = new decimal[12];
    public decimal ThisMonthVsLastMonthPercent { get; set; }
    public decimal ThisMonthVsLastYearPercent { get; set; }
}

public class UserKPI
{
    public string UserCode { get; set; } = "";
    public string UserName { get; set; } = "";
    public bool IsDirector { get; set; }
    public int QuotesRaisedThisMonth { get; set; }
    public int QuotesWonThisMonth { get; set; }
    public decimal QuoteValueThisMonth { get; set; }
    public int InvoicesClosedThisMonth { get; set; }
    public decimal InvoiceValueThisMonth { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageQuoteValue { get; set; }
    public int PendingQuotes { get; set; }
    public int OverdueQuotes { get; set; }
    public decimal YtdRevenue { get; set; }
    public int Rank { get; set; }
    public string PerformanceTrend { get; set; } = "stable"; // up, down, stable

    // ── Target Tracking ────────────────────────────────────────────────────────
    public decimal MonthlyTarget { get; set; }
    public decimal QuarterlyTarget { get; set; }
    public decimal YearlyTarget { get; set; }
    public decimal MonthlyProgress => MonthlyTarget > 0 ? (InvoiceValueThisMonth / MonthlyTarget) * 100 : 0;
    public decimal QuarterlyProgress { get; set; }
    public decimal YearlyProgress => YearlyTarget > 0 ? (YtdRevenue / YearlyTarget) * 100 : 0;
    public decimal QuarterlyRevenue { get; set; }
    public string PerformanceBand => MonthlyProgress switch
    {
        >= 110 => "exceeding",
        >= 90 => "on-track",
        >= 70 => "at-risk",
        _ => "behind"
    };
}

// ============================================================================
// Targets & Performance Models
// ============================================================================
public class PerformanceTargets
{
    // Company-wide
    public decimal CompanyMonthlyTarget { get; set; }
    public decimal CompanyQuarterlyTarget { get; set; }
    public decimal CompanyYearlyTarget { get; set; }

    public decimal CompanyMonthlyActual { get; set; }
    public decimal CompanyQuarterlyActual { get; set; }
    public decimal CompanyYearlyActual { get; set; }

    // Sales Team (aggregate of all sales users)
    public decimal TeamMonthlyTarget { get; set; }
    public decimal TeamQuarterlyTarget { get; set; }
    public decimal TeamYearlyTarget { get; set; }

    public decimal TeamMonthlyActual { get; set; }
    public decimal TeamQuarterlyActual { get; set; }
    public decimal TeamYearlyActual { get; set; }

    // Forecasts
    public decimal MonthlyForecast { get; set; }     // Projected month-end actual
    public decimal QuarterlyForecast { get; set; }
    public decimal YearlyForecast { get; set; }

    // Helpers
    public decimal MonthlyProgress => CompanyMonthlyTarget > 0 ? (CompanyMonthlyActual / CompanyMonthlyTarget) * 100 : 0;
    public decimal QuarterlyProgress => CompanyQuarterlyTarget > 0 ? (CompanyQuarterlyActual / CompanyQuarterlyTarget) * 100 : 0;
    public decimal YearlyProgress => CompanyYearlyTarget > 0 ? (CompanyYearlyActual / CompanyYearlyTarget) * 100 : 0;

    public int DaysIntoMonth { get; set; }
    public int DaysInMonth { get; set; }
    public int DaysIntoQuarter { get; set; }
    public int DaysInQuarter { get; set; }
    public int DaysIntoYear { get; set; }
    public int DaysInYear { get; set; }

    public decimal ExpectedMonthlyProgress => DaysInMonth > 0 ? ((decimal)DaysIntoMonth / DaysInMonth) * 100 : 0;
    public decimal ExpectedQuarterlyProgress => DaysInQuarter > 0 ? ((decimal)DaysIntoQuarter / DaysInQuarter) * 100 : 0;
    public decimal ExpectedYearlyProgress => DaysInYear > 0 ? ((decimal)DaysIntoYear / DaysInYear) * 100 : 0;
}

// ============================================================================
// Customer Intelligence Models
// ============================================================================
public class CustomerIntelligence
{
    public List<CustomerPerformance> BestCustomers { get; set; } = new();
    public List<CustomerPerformance> WorstCustomers { get; set; } = new();
    public List<CustomerPerformance> AtRiskCustomers { get; set; } = new();   // declining
    public List<CustomerPerformance> GrowthCustomers { get; set; } = new();   // fast growing
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }      // invoiced in last 90 days
    public int DormantCustomers { get; set; }     // no activity in 180+ days
    public decimal AverageCustomerValue { get; set; }
    public decimal Top10CustomerConcentration { get; set; }   // % of revenue from top 10
}

public class CustomerPerformance
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public decimal YtdRevenue { get; set; }
    public decimal LastYearRevenue { get; set; }
    public decimal LifetimeRevenue { get; set; }
    public int InvoiceCount { get; set; }
    public int QuoteCount { get; set; }
    public decimal QuoteValue { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public decimal WinRate { get; set; }                 // quotes → invoices
    public decimal GrowthPercent { get; set; }           // YoY
    public DateTime? LastActivity { get; set; }
    public int DaysSinceLastActivity { get; set; }
    public decimal EffortToValueRatio { get; set; }      // quotes made vs revenue (low = efficient)
    public string Rating { get; set; } = "";             // Diamond, Gold, Silver, Bronze, Watch
    public int Rank { get; set; }
    public decimal OutstandingReceivables { get; set; }
    public int OverdueInvoices { get; set; }
}

// ============================================================================
// Team Leaderboard
// ============================================================================
public class TeamLeaderboard
{
    public List<UserKPI> Members { get; set; } = new();
    public UserKPI? TopPerformer { get; set; }
    public UserKPI? MostImproved { get; set; }
    public decimal TeamAverageWinRate { get; set; }
    public decimal TeamAverageRevenue { get; set; }
    public decimal TeamTotalRevenue { get; set; }
    public int TotalMembers { get; set; }
    public int MembersMeetingTarget { get; set; }
}

public class DivisionPerformance
{
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = "";
    public decimal ThisMonthRevenue { get; set; }
    public decimal YtdRevenue { get; set; }
    public int QuotesCount { get; set; }
    public int InvoicesCount { get; set; }
    public decimal GrowthPercent { get; set; }
    public int HealthScore { get; set; }
}

public class BusinessWarning
{
    public string Id { get; set; } = "";
    public string Severity { get; set; } = "warning"; // warning, critical
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Metric { get; set; } = "";
    public decimal Threshold { get; set; }
    public decimal CurrentValue { get; set; }
    public string ActionLink { get; set; } = "";
}

public class BusinessRecommendation
{
    public string Id { get; set; } = "";
    public string Priority { get; set; } = "medium"; // high, medium, low
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ExpectedImpact { get; set; } = "";
    public string ActionLink { get; set; } = "";
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

public class JobOrderStatus
{
    public int JobOrderStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class CurrencyRate
{
    public int CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; } = 1m;
}

public class SystemParameters
{
    public int ParameterId { get; set; }
    public DateTime? UploadFrom { get; set; }
    public decimal MinimumValue { get; set; }
}

// ============================================================================
// Shared / UI Helper Models
// ============================================================================
public class CommentItem
{
    public int CommentId { get; set; }
    public string CommentText { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserCode { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UploadedFile
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

public class JobOrderLineItemEdit
{
    public int Qty { get; set; }
    public string Description { get; set; } = "";
    public string? CategoryName { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
}

public class BrandAssetFile
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsPublic { get; set; } = true;
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    
    public string FormattedSize => FormatFileSize(FileSize);
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }
}

public class ImportantLink
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Category { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsExternal { get; set; }
    public bool IsActive { get; set; } = true;
}

public class IntegrationStatus
{
    public string ServiceName { get; set; } = "";
    public bool IsConnected { get; set; }
    public string? LastSync { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? LastTested { get; set; }
    public bool LastTestSuccess { get; set; }
    public string? LastTestMessage { get; set; }
}

// ============================================================================
// Supplier Portal Model
// ============================================================================
public class Supplier
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ABN { get; set; }
    public string? Category { get; set; }
    public string? Region { get; set; } = "NSW";
    
    // Portal Credentials
    public string? PortalUsername { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool IsPortalEnabled { get; set; }
    public DateTime? PortalLastLogin { get; set; }
    public DateTime? PortalAccessExpires { get; set; }
    
    // Scoring & Tiering
    public int Score { get; set; } = 50;
    public string Tier { get; set; } = "Bronze";
    public decimal TotalSpend { get; set; }
    public int OrderCount { get; set; }
    public double OnTimePercent { get; set; }
    public double QualityPercent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class SupplierScore
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public int Score { get; set; }
    public string Tier { get; set; } = "";
    public decimal TotalSpend { get; set; }
    public int OrdersThisYear { get; set; }
    public double OnTimeDelivery { get; set; }
    public double QualityRating { get; set; }
    public DateTime LastOrderDate { get; set; }
}

public class CustomerFile
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = "";
    public string FileSize { get; set; } = "";
    public DateTime UploadedDate { get; set; }
    public int CompanyId { get; set; }
}

public class ApplicationLog
{
    public long LogId { get; set; }
    public Guid TenantId { get; set; }
    public string LogLevel { get; set; } = "INFO";      // INFO, WARNING, ERROR, CRITICAL
    public string? LogCategory { get; set; }
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? UserCode { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

