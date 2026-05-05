using System;
using System.Collections.Generic;

namespace MyDesk.Shared.Models;

// ============================================================================
// Approval chain settings — companion to User (kept here so the Users table
// schema does NOT have to change). Stored in-memory by the service layer.
// ============================================================================
public class UserApprovalSettings
{
    /// <summary>UserCode of this user's line manager (the next approver in the chain).</summary>
    public string UserCode { get; set; } = string.Empty;
    public string? LineManagerCode { get; set; }
    /// <summary>True if this user can approve CapEx items in the PO chain.</summary>
    public bool IsCapExApprover { get; set; }
}

// ============================================================================
// Approval entries recorded against quotes / POs (in-memory list per item).
// ============================================================================
public class ApprovalEntry
{
    public int EntryId { get; set; }
    public string EntityType { get; set; } = string.Empty; // "Quote" | "PurchaseOrder"
    public int EntityId { get; set; }
    public int Level { get; set; }
    public string ApproverCode { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public string Decision { get; set; } = "Approved"; // Approved | Declined
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
}

// ============================================================================
// Pending approval row used by /approvals/pending and the dashboard widget.
// ============================================================================
public class PendingApprovalItem
{
    public string EntityType { get; set; } = string.Empty; // Quote | PurchaseOrder
    public int EntityId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Originator { get; set; } = string.Empty;
    public string OriginatorCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int CompletedLevels { get; set; }
    public string? LastApprover { get; set; }
    public DateTime? LastApprovalAt { get; set; }
    public bool HasCapEx { get; set; }
    public string ViewUrl => EntityType == "Quote"
        ? $"/quotes/{EntityId}"
        : $"/purchase-orders/{EntityId}";
}

// ============================================================================
// Timesheet missing report row (Module B)
// ============================================================================
public class TimesheetMissingDto
{
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? DivisionId { get; set; }
    public string? DivisionName { get; set; }
}

public class TimesheetLineApproval
{
    public int TimesheetId { get; set; }
    public int LineId { get; set; }
    public string ApproverCode { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; } = DateTime.Now;
    public bool Approved { get; set; } = true; // false = rejected
}

// ============================================================================
// Sales reports DTOs (Module C)
// ============================================================================
public class MonthlySalesDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => new DateTime(Year, Month, 1).ToString("MMM yy");
    public decimal QuoteTotal { get; set; }
    public decimal InvoiceTotal { get; set; }
}

public class RepSalesDto
{
    public string OwnerUserCode { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class DivisionSalesDto
{
    public int DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class YearOnYearDto
{
    public int CurrentYear { get; set; }
    public int PreviousYear { get; set; }
    /// <summary>12 entries (Jan..Dec) for the current calendar year.</summary>
    public decimal[] CurrentYearMonthly { get; set; } = new decimal[12];
    public decimal[] PreviousYearMonthly { get; set; } = new decimal[12];
}

public class QuoteStatusSummaryDto
{
    /// <summary>12 entries (last 12 months oldest -> newest).</summary>
    public string[] Labels { get; set; } = new string[12];
    public decimal[] Pending { get; set; } = new decimal[12];
    public decimal[] Won { get; set; } = new decimal[12];
}
