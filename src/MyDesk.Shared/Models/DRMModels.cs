using System;

namespace MyDesk.Shared.Models;

// ============================================================================
// Subscription Management Models (from DRM.xlsx)
// ============================================================================

public class DRMSubscription
{
    public int SubscriptionId { get; set; }
    public string ClientName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "Hosting";
    public string Schedule { get; set; } = "Monthly";
    public decimal AmountInclGST { get; set; }
    public decimal AmountExGST { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? NextInvoiceDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public decimal? ApproxCost { get; set; }
    public string? LoginDetails { get; set; }
    public string? InvoiceLink { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SubscriptionInvoice
{
    public int SubInvoiceId { get; set; }
    public int SubscriptionId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public decimal AmountInclGST { get; set; }
    public decimal AmountExGST { get; set; }
    public decimal GSTAmount { get; set; }
    public string? PaidVia { get; set; }
    public bool IsClaimed { get; set; }
    public string? ClaimedInExpenseReport { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// Charge Models (from DRM.xlsx Charges sheet)
// ============================================================================

public class DRMCharge
{
    public int ChargeId { get; set; }
    public DateTime ChargeDate { get; set; }
    public string ClientName { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsInvoiced { get; set; }
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public int? DRMProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ============================================================================
// Expense Report Models (replaces Excel monthly tabs)
// ============================================================================

public class ExpenseReport
{
    public int ReportId { get; set; }
    public DateTime ReportPeriod { get; set; }
    public string Status { get; set; } = "Draft";
    public int? SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReimbursedDate { get; set; }
    public decimal? ReimbursementAmount { get; set; }
    public string? ReimbursementNotes { get; set; }
    public decimal TotalExGST { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalInclGST { get; set; }
    public string OwnerType { get; set; } = "DR";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExpenseReportLine
{
    public int LineId { get; set; }
    public int ReportId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
    public decimal AmountExGST { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal AmountInclGST { get; set; }
    public string OwnerType { get; set; } = "DR";
    public string? Classification { get; set; }
    public bool HasReceipt { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptFilePath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// O365 Subscription Models (from DRM.xlsx O365 sheet)
// ============================================================================

public class O365Subscription
{
    public int O365SubId { get; set; }
    public string ServiceName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? UserName { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public DateTime? DateCommenced { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ============================================================================
// System Credentials Models (from DRM.xlsx Passwords sheet)
// ============================================================================

public class SystemCredential
{
    public int CredentialId { get; set; }
    public string SiteName { get; set; } = "";
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
