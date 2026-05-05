using System;
using System.Collections.Generic;

namespace MyDesk.Shared.Models;

/// <summary>
/// Parent table for every uploaded financial document (Quote, Invoice, Receipt).
/// Matches the PRD "Documents" table specification.
/// </summary>
public class FinancialDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public int DocumentType { get; set; } // 0=Quote, 1=Invoice, 2=Receipt, 3=PurchaseOrder
    public int Status { get; set; } // 0=Draft, 1=Verified, 2=Processed, 3=RequiresCorrection
    public string? SourceUrl { get; set; }
    public string ExtractionMethod { get; set; } = ""; // "Deterministic" or "GPT-5.4-Mini"
    public double ConfidenceScore { get; set; }
    public bool AuditPassed { get; set; }
    public string? DiscrepanciesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Denormalized from FinancialMetadata for list views
    public string? SupplierName { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "AUD";
}

/// <summary>
/// High-level extracted data. Maps to PRD "FinancialMetadata" table.
/// </summary>
public class FinancialMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierAbn { get; set; }
    public string? SupplierEmail { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Currency { get; set; } = "AUD";
    public decimal Subtotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RawText { get; set; }
}

/// <summary>
/// Line items. Maps to PRD "LineItems" table.
/// Wholesale often uses 4 decimals for bulk weights — hence decimal(18,4) for Qty/UnitPrice.
/// </summary>
public class FinancialLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ProductCode { get; set; }
}

/// <summary>
/// Display-friendly document type names.
/// </summary>
public static class FinancialDocumentTypes
{
    public const int Quote = 0;
    public const int Invoice = 1;
    public const int Receipt = 2;
    public const int PurchaseOrder = 3;

    public static string ToString(int type) => type switch
    {
        Quote => "Quote",
        Invoice => "Invoice",
        Receipt => "Receipt",
        PurchaseOrder => "Purchase Order",
        _ => "Unknown"
    };
}

/// <summary>
/// Display-friendly status names.
/// </summary>
public static class FinancialDocumentStatus
{
    public const int Draft = 0;
    public const int Verified = 1;
    public const int Processed = 2;
    public const int RequiresCorrection = 3;

    public static string ToString(int status) => status switch
    {
        Draft => "Draft",
        Verified => "Verified",
        Processed => "Processed",
        RequiresCorrection => "Requires Correction",
        _ => "Unknown"
    };
}
